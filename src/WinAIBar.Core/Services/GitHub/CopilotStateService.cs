using WinAIBar.Core.Models;
using WinAIBar.Core.Services.Anthropic;

namespace WinAIBar.Core.Services.GitHub;

public sealed class CopilotStateService : ICopilotStateService, IDisposable
{
    private volatile ProviderSnapshot? _latestSnapshot;
    private volatile ProviderHealth? _latestHealth;
    private readonly SimpleSubject<ProviderSnapshot> _subject = new();

    public ProviderSnapshot? LatestSnapshot => _latestSnapshot;
    public ProviderHealth? LatestHealth => _latestHealth;
    public IObservable<ProviderSnapshot> SnapshotStream => _subject;

    public void UpdateSnapshot(ProviderSnapshot snapshot)
    {
        _latestSnapshot = snapshot;
        _subject.OnNext(snapshot);
    }

    public void UpdateHealth(ProviderHealth health) => _latestHealth = health;

    public void Dispose() => _subject.Dispose();
}
