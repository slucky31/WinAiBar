using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using WinAIBar.Core.Services.GitHub.Dto;

namespace WinAIBar.Core.Services.GitHub;

public sealed partial class GitHubDeviceCodeAuthenticator : IGitHubDeviceCodeAuthenticator
{
    private readonly HttpClient _httpClient;
    private readonly GitHubOAuthOptions _options;
    private readonly ILogger<GitHubDeviceCodeAuthenticator> _logger;

    public GitHubDeviceCodeAuthenticator(
        HttpClient httpClient,
        GitHubOAuthOptions options,
        ILogger<GitHubDeviceCodeAuthenticator> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
    }

    public async Task<DeviceCodeResponse> StartAsync(CancellationToken ct)
    {
        var formData = new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId,
            ["scope"] = _options.Scope
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, _options.DeviceCodeUrl)
        {
            Content = new FormUrlEncodedContent(formData)
        };
        request.Headers.Accept.ParseAdd("application/json");

        var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<DeviceCodeResponse>(ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Empty device code response from GitHub.");

        LogDeviceCodeReceived(_logger, result.UserCode, result.VerificationUri);
        return result;
    }

    public async Task<string> PollForTokenAsync(DeviceCodeResponse start, IProgress<string>? progress, CancellationToken ct)
    {
        var intervalSeconds = start.Interval;

        while (true)
        {
            ct.ThrowIfCancellationRequested();
            await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), ct).ConfigureAwait(false);

            var formData = new Dictionary<string, string>
            {
                ["client_id"] = _options.ClientId,
                ["device_code"] = start.DeviceCode,
                ["grant_type"] = "urn:ietf:params:oauth:grant-type:device_code"
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, _options.TokenUrl)
            {
                Content = new FormUrlEncodedContent(formData)
            };
            request.Headers.Accept.ParseAdd("application/json");

            var response = await _httpClient.SendAsync(request, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var tokenResponse = await response.Content.ReadFromJsonAsync<AccessTokenResponse>(ct).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Empty token response from GitHub.");

            if (tokenResponse.AccessToken is not null)
            {
                LogTokenReceived(_logger);
                return tokenResponse.AccessToken;
            }

            switch (tokenResponse.Error)
            {
                case "authorization_pending":
                    progress?.Report("authorization_pending");
                    LogAuthorizationPending(_logger);
                    break;
                case "slow_down":
                    intervalSeconds += 5;
                    progress?.Report("slow_down");
                    LogSlowDown(_logger, intervalSeconds);
                    break;
                case "expired_token":
                    throw new DeviceCodeExpiredException("The device code has expired.");
                case "access_denied":
                    throw new DeviceCodeAccessDeniedException("The user denied access.");
                default:
                    throw new InvalidOperationException($"Unexpected GitHub OAuth error: {tokenResponse.Error}");
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "GitHub device code received. UserCode: {UserCode}, URL: {VerificationUri}")]
    private static partial void LogDeviceCodeReceived(ILogger logger, string userCode, string verificationUri);

    [LoggerMessage(Level = LogLevel.Information, Message = "GitHub access token received successfully")]
    private static partial void LogTokenReceived(ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "GitHub authorization pending, retrying...")]
    private static partial void LogAuthorizationPending(ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "GitHub slow_down received, new interval: {IntervalSeconds}s")]
    private static partial void LogSlowDown(ILogger logger, int intervalSeconds);
}
