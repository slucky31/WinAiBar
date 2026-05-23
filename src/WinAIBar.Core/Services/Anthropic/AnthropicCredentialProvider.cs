using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace WinAIBar.Core.Services.Anthropic;

public sealed partial class AnthropicCredentialProvider : IAnthropicCredentialProvider
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

    private readonly Func<string, string?> _fileReader;
    private readonly ILogger<AnthropicCredentialProvider> _logger;
    private readonly string _credentialsPath;
    private readonly object _lock = new();

    private AnthropicCredentials? _cached;
    private DateTimeOffset _cacheExpiry = DateTimeOffset.MinValue;

    public AnthropicCredentialProvider(
        Func<string, string?> fileReader,
        ILogger<AnthropicCredentialProvider> logger)
    {
        _fileReader = fileReader;
        _logger = logger;
        _credentialsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".claude",
            ".credentials.json");
    }

    public Task<AnthropicCredentials?> GetAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        lock (_lock)
        {
            var now = DateTimeOffset.UtcNow;
            if (now < _cacheExpiry)
                return Task.FromResult(_cached);

            _cached = LoadCredentials(now);
            _cacheExpiry = now.Add(CacheDuration);
            return Task.FromResult(_cached);
        }
    }

    public bool IsAvailable()
    {
        lock (_lock)
        {
            var now = DateTimeOffset.UtcNow;
            if (now < _cacheExpiry)
                return _cached is not null;

            _cached = LoadCredentials(now);
            _cacheExpiry = now.Add(CacheDuration);
            return _cached is not null;
        }
    }

    private AnthropicCredentials? LoadCredentials(DateTimeOffset now)
    {
        string? content;
        try
        {
            content = _fileReader(_credentialsPath);
        }
        catch (Exception ex)
        {
            LogReadError(_logger, ex);
            return null;
        }

        if (content is null)
        {
            LogFileNotFound(_logger, _credentialsPath);
            return null;
        }

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(content);
        }
        catch (JsonException ex)
        {
            LogInvalidJson(_logger, ex);
            return null;
        }

        using (doc)
        {
            if (!doc.RootElement.TryGetProperty("claudeAiOauth", out var oauth))
            {
                LogMissingSection(_logger);
                return null;
            }

            if (!oauth.TryGetProperty("accessToken", out var accessTokenEl) ||
                string.IsNullOrEmpty(accessTokenEl.GetString()))
            {
                LogMissingAccessToken(_logger);
                return null;
            }

            var accessToken = accessTokenEl.GetString()!;
            string? refreshToken = oauth.TryGetProperty("refreshToken", out var rtEl) ? rtEl.GetString() : null;

            DateTimeOffset? expiresAt = null;
            if (oauth.TryGetProperty("expiresAt", out var expiresAtEl) &&
                expiresAtEl.TryGetInt64(out var expiresAtMs))
            {
                expiresAt = DateTimeOffset.FromUnixTimeMilliseconds(expiresAtMs);
                // Grace period: treat the token as expired only after 60s past its expiry
                // to tolerate minor clock skew between the machine and Anthropic servers.
                if (expiresAt.Value < now.AddSeconds(-60))
                {
                    LogTokenExpired(_logger, expiresAt.Value);
                    return null;
                }
            }

            LogCredentialsLoaded(_logger);
            return new AnthropicCredentials(accessToken, refreshToken, expiresAt);
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to read Anthropic credentials file")]
    private static partial void LogReadError(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Anthropic credentials file not found: {Path}")]
    private static partial void LogFileNotFound(ILogger logger, string path);

    [LoggerMessage(Level = LogLevel.Error, Message = "Anthropic credentials file contains invalid JSON")]
    private static partial void LogInvalidJson(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Anthropic credentials file missing 'claudeAiOauth' section")]
    private static partial void LogMissingSection(ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Anthropic credentials file missing or empty 'accessToken'")]
    private static partial void LogMissingAccessToken(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Anthropic token expired at {ExpiresAt}")]
    private static partial void LogTokenExpired(ILogger logger, DateTimeOffset expiresAt);

    [LoggerMessage(Level = LogLevel.Information, Message = "Anthropic credentials loaded")]
    private static partial void LogCredentialsLoaded(ILogger logger);
}
