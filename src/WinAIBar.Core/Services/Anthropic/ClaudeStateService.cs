using WinAIBar.Core.Models;

namespace WinAIBar.Core.Services.Anthropic;

public sealed class ClaudeStateService : IClaudeStateService, IDisposable
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

internal sealed class SimpleSubject<T> : IObservable<T>, IDisposable
{
    private readonly object _lock = new();
    private readonly List<IObserver<T>> _observers = [];
    private bool _disposed;

    public IDisposable Subscribe(IObserver<T> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _observers.Add(observer);
        }
        return new Subscription(this, observer);
    }

    public void OnNext(T value)
    {
        IObserver<T>[] observers;
        lock (_lock)
        {
            if (_disposed) return;
            observers = [.. _observers];
        }
        foreach (var o in observers)
            o.OnNext(value);
    }

    public void Dispose()
    {
        IObserver<T>[] observers;
        lock (_lock)
        {
            if (_disposed) return;
            _disposed = true;
            observers = [.. _observers];
            _observers.Clear();
        }
        foreach (var o in observers)
            o.OnCompleted();
    }

    private void Remove(IObserver<T> observer)
    {
        lock (_lock) { _observers.Remove(observer); }
    }

    private sealed class Subscription(SimpleSubject<T> subject, IObserver<T> observer) : IDisposable
    {
        private int _disposed;
        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
                subject.Remove(observer);
        }
    }
}
