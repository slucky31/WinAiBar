using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace WinAIBar.Core.Services.GitHub;

[SupportedOSPlatform("windows")]
public sealed partial class GitHubTokenStore : IGitHubTokenStore
{
    private readonly string _tokenFilePath;
    private readonly ILogger<GitHubTokenStore> _logger;

    public GitHubTokenStore(string tokenFilePath, ILogger<GitHubTokenStore> logger)
    {
        _tokenFilePath = tokenFilePath;
        _logger = logger;
    }

    public bool HasToken() => File.Exists(_tokenFilePath) && new FileInfo(_tokenFilePath).Length > 0;

    public async Task SaveAsync(string token, CancellationToken ct)
    {
        var plainBytes = Encoding.UTF8.GetBytes(token);
        var encrypted = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);

        var dir = Path.GetDirectoryName(_tokenFilePath);
        if (dir is not null)
            Directory.CreateDirectory(dir);

        var tmpPath = _tokenFilePath + ".tmp";
        await File.WriteAllBytesAsync(tmpPath, encrypted, ct).ConfigureAwait(false);
        File.Move(tmpPath, _tokenFilePath, overwrite: true);

        LogTokenSaved(_logger);
    }

    public async Task<string?> LoadAsync(CancellationToken ct)
    {
        if (!HasToken())
            return null;

        try
        {
            var encrypted = await File.ReadAllBytesAsync(_tokenFilePath, ct).ConfigureAwait(false);
            var plainBytes = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (Exception ex)
        {
            LogTokenLoadFailed(_logger, ex);
            return null;
        }
    }

    public async Task ClearAsync(CancellationToken ct)
    {
        if (!File.Exists(_tokenFilePath))
            return;

        await Task.Run(() => File.Delete(_tokenFilePath), ct).ConfigureAwait(false);
        LogTokenCleared(_logger);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "GitHub token saved to encrypted store")]
    private static partial void LogTokenSaved(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to load GitHub token from encrypted store")]
    private static partial void LogTokenLoadFailed(ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "GitHub token cleared from encrypted store")]
    private static partial void LogTokenCleared(ILogger logger);
}
