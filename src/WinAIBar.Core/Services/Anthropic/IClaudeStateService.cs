using WinAIBar.Core.Models;

namespace WinAIBar.Core.Services.Anthropic;

public interface IClaudeStateService
{
    ProviderSnapshot? LatestSnapshot { get; }
    ProviderHealth? LatestHealth { get; }
    IObservable<ProviderSnapshot> SnapshotStream { get; }
    void UpdateSnapshot(ProviderSnapshot snapshot);
    void UpdateHealth(ProviderHealth health);
}
