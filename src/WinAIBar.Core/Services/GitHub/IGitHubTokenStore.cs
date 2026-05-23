namespace WinAIBar.Core.Services.GitHub;

public interface IGitHubTokenStore
{
    Task SaveAsync(string token, CancellationToken ct);
    Task<string?> LoadAsync(CancellationToken ct);
    Task ClearAsync(CancellationToken ct);
    bool HasToken();
}
