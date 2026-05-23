using WinAIBar.Core.Models;

namespace WinAIBar.Core.Providers;

public interface IProviderClient
{
    ProviderId ProviderId { get; }
    Task<ProviderSnapshot> FetchSnapshotAsync(CancellationToken ct);
}
