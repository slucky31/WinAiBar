using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using WinAIBar.Core.Services.GitHub;
using WinAIBar.Core.Services.GitHub.Dto;
using Xunit;

namespace WinAIBar.Tests.Services.GitHub;

public sealed class GitHubDeviceCodeAuthenticatorTests
{
    private static readonly GitHubOAuthOptions Options = new();

    private static (GitHubDeviceCodeAuthenticator Authenticator, HttpClient HttpClient) CreateAuthenticator(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler);
        var authenticator = new GitHubDeviceCodeAuthenticator(httpClient, Options, NullLogger<GitHubDeviceCodeAuthenticator>.Instance);
        return (authenticator, httpClient);
    }

    private static StringContent JsonContent(object value) =>
        new(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json");

    [Fact]
    public async Task StartAsyncReturnsDeviceCodeOnSuccess()
    {
        var payload = new
        {
            device_code = "dev-abc",
            user_code = "USER-CODE",
            verification_uri = "https://github.com/login/device",
            expires_in = 900,
            interval = 5
        };
        using var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent(payload) };
        using var handler = new SingleResponseHandler(response);
        var (authenticator, httpClient) = CreateAuthenticator(handler);
        using (httpClient)
        {
            var result = await authenticator.StartAsync(TestContext.Current.CancellationToken);

            Assert.Equal("dev-abc", result.DeviceCode);
            Assert.Equal("USER-CODE", result.UserCode);
            Assert.Equal("https://github.com/login/device", result.VerificationUri);
            Assert.Equal(900, result.ExpiresIn);
            Assert.Equal(5, result.Interval);
        }
    }

    [Fact]
    public async Task PollForTokenAsyncReturnTokenAfterPending()
    {
        var pendingPayload = new { error = "authorization_pending" };
        var successPayload = new { access_token = "gho_mytoken", token_type = "bearer", scope = "read:email" };

        using var first = new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent(pendingPayload) };
        using var second = new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent(successPayload) };
        using var handler = new QueuedResponseHandler([first, second]);
        var (authenticator, httpClient) = CreateAuthenticator(handler);
        using (httpClient)
        {
            var start = new DeviceCodeResponse("dev-xyz", "ABCD-1234", "https://github.com/login/device", 900, 0);

            var token = await authenticator.PollForTokenAsync(start, null, TestContext.Current.CancellationToken);

            Assert.Equal("gho_mytoken", token);
            Assert.Equal(2, handler.CallCount);
        }
    }

    [Fact]
    public async Task PollForTokenAsyncThrowsOnExpiredToken()
    {
        var expiredPayload = new { error = "expired_token" };
        using var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent(expiredPayload) };
        using var handler = new SingleResponseHandler(response);
        var (authenticator, httpClient) = CreateAuthenticator(handler);
        using (httpClient)
        {
            var start = new DeviceCodeResponse("dev-expired", "AAAA-BBBB", "https://github.com/login/device", 5, 0);

            await Assert.ThrowsAsync<DeviceCodeExpiredException>(
                () => authenticator.PollForTokenAsync(start, null, TestContext.Current.CancellationToken));
        }
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
