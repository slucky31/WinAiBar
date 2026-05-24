namespace WinAIBar.Core.Infrastructure;

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
