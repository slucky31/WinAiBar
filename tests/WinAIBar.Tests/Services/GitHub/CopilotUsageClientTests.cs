using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WinAIBar.Core.Models;
using WinAIBar.Core.Services.GitHub;
using Xunit;

namespace WinAIBar.Tests.Services.GitHub;

public sealed class CopilotUsageClientTests
{
    private static readonly string CopilotInternalJson = File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "TestData", "copilot-internal-200.json"));

    private static readonly string CopilotPremiumUsageJson = File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "TestData", "copilot-premium-usage-200.json"));

    private static IGitHubTokenStore CreateTokenStore(string? token = "test-github-token")
    {
        var store = Substitute.For<IGitHubTokenStore>();
        store.LoadAsync(Arg.Any<CancellationToken>()).Returns(token);
        return store;
    }

    [Fact]
    public async Task FetchAsyncReturnsCompleteSnapshotOn200()
    {
        using var firstResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(CopilotInternalJson, Encoding.UTF8, "application/json")
        };
        using var secondResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(CopilotPremiumUsageJson, Encoding.UTF8, "application/json")
        };
        using var handler = new QueuedResponseHandler([firstResponse, secondResponse]);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.github.com") };
        var client = new CopilotUsageClient(httpClient, CreateTokenStore(), NullLogger<CopilotUsageClient>.Instance);

        var snapshot = await client.FetchAsync(TestContext.Current.CancellationToken);

        Assert.Equal(ProviderId.Copilot, snapshot.Provider);
        Assert.Equal(2, snapshot.Quotas.Count);
        Assert.NotNull(snapshot.RawPayload);
        Assert.Contains(snapshot.Quotas, q => q.Key == "premium-requests");
        Assert.Contains(snapshot.Quotas, q => q.Key == "credits");
    }

    [Fact]
    public async Task FetchAsyncReturnsDegradedSnapshotOn404()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.NotFound);
        using var handler = new SingleResponseHandler(response);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.github.com") };
        var client = new CopilotUsageClient(httpClient, CreateTokenStore(), NullLogger<CopilotUsageClient>.Instance);

        var snapshot = await client.FetchAsync(TestContext.Current.CancellationToken);

        Assert.Equal(ProviderId.Copilot, snapshot.Provider);
        Assert.Single(snapshot.Quotas);
        Assert.Equal("endpoint_missing", snapshot.Quotas[0].Key);
    }

    [Fact]
    public async Task FetchAsyncThrowsUnauthorizedOn401()
    {
        using var response = new HttpResponseMessage(HttpStatusCode.Unauthorized);
        using var handler = new SingleResponseHandler(response);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.github.com") };
        var client = new CopilotUsageClient(httpClient, CreateTokenStore(), NullLogger<CopilotUsageClient>.Instance);

        await Assert.ThrowsAsync<CopilotUnauthorizedException>(
            () => client.FetchAsync(TestContext.Current.CancellationToken));
    }

    private sealed class SingleResponseHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(response);
    }

    private sealed class QueuedResponseHandler(IEnumerable<HttpResponseMessage> responses) : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _queue = new(responses);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => Task.FromResult(_queue.Dequeue());
    }
}
