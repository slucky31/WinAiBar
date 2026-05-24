using WinAIBar.Core.Models;

namespace WinAIBar.Core.Services.GitHub;

public interface ICopilotStateService
{
    ProviderSnapshot? LatestSnapshot { get; }
    ProviderHealth? LatestHealth { get; }
    IObservable<ProviderSnapshot> SnapshotStream { get; }
    void UpdateSnapshot(ProviderSnapshot snapshot);
    void UpdateHealth(ProviderHealth health);
}
