using WinAIBar.Core.Models;

namespace WinAIBar.Core.Services.GitHub;

public interface ICopilotUsageClient
{
    Task<ProviderSnapshot> FetchAsync(CancellationToken ct = default);
}
