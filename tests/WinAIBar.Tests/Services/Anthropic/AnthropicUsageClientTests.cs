using System.Net;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Polly;
using WinAIBar.Core.Models;
using WinAIBar.Core.Services.Anthropic;
using Xunit;

namespace WinAIBar.Tests.Services.Anthropic;

public sealed class AnthropicUsageClientTests
{
    private static readonly string ValidJson = File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "TestData", "anthropic-usage-200.json"));

    private static IAnthropicCredentialProvider CreateCredProvider()
    {
        var provider = Substitute.For<IAnthropicCredentialProvider>();
        provider.GetAsync(Arg.Any<CancellationToken>())
            .Returns(new AnthropicCredentials("test-access-token", null, null));
        return provider;
    }

    [Fact]
    public async Task FetchAsyncReturnsSnapshotWithQuotasOn200()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(ValidJson, Encoding.UTF8, "application/json")
        };
        using var handler = new SingleResponseHandler(response);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.anthropic.com") };
        var client = new AnthropicUsageClient(httpClient, CreateCredProvider(), NullLogger<AnthropicUsageClient>.Instance);

        var snapshot = await client.FetchAsync(TestContext.Current.CancellationToken);

        Assert.Equal(ProviderId.Claude, snapshot.Provider);
        Assert.Equal(2, snapshot.Quotas.Count);
        Assert.NotNull(snapshot.RawPayload);

        var sessionQuota = snapshot.Quotas.FirstOrDefault(q => q.Key == "session-5h");
        Assert.NotNull(sessionQuota);
        Assert.Equal(0.42, sessionQuota!.Utilization, precision: 10);
        Assert.Equal(210000L, sessionQuota.Used);
        Assert.Equal(500000L, sessionQuota.Limit);
    }

    [Fact]
    public async Task FetchAsyncThrowsUnauthorizedOn401()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.Unauthorized);
        using var handler = new SingleResponseHandler(response);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.anthropic.com") };
        var client = new AnthropicUsageClient(httpClient, CreateCredProvider(), NullLogger<AnthropicUsageClient>.Instance);

        await Assert.ThrowsAsync<AnthropicUnauthorizedException>(
            () => client.FetchAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task FetchAsyncReturnsSnapshotAfterRetryOn429()
    {
        using var first = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
        using var second = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(ValidJson, Encoding.UTF8, "application/json")
        };
        using var innerHandler = new QueuedResponseHandler([first, second]);

        var services = new ServiceCollection();
        services.AddSingleton<IAnthropicCredentialProvider>(CreateCredProvider());
        services.AddSingleton<ILogger<AnthropicUsageClient>>(NullLogger<AnthropicUsageClient>.Instance);
        services.AddHttpClient<IAnthropicUsageClient, AnthropicUsageClient>(client =>
                client.BaseAddress = new Uri("https://api.anthropic.com"))
            .ConfigurePrimaryHttpMessageHandler(() => innerHandler)
            .AddResilienceHandler("test", builder =>
                builder.AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = 1,
                    Delay = TimeSpan.Zero,
                    BackoffType = DelayBackoffType.Constant,
                    UseJitter = false,
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .HandleResult(r => r.StatusCode == HttpStatusCode.TooManyRequests)
                }));

        await using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<IAnthropicUsageClient>();

        var snapshot = await client.FetchAsync(TestContext.Current.CancellationToken);

        Assert.Equal(ProviderId.Claude, snapshot.Provider);
        Assert.Equal(2, innerHandler.CallCount);
    }

    private sealed class SingleResponseHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(response);
    }

    private sealed class QueuedResponseHandler(IEnumerable<HttpResponseMessage> responses) : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _queue = new(responses);
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            CallCount++;
            return Task.FromResult(_queue.Dequeue());
        }
    }
}
