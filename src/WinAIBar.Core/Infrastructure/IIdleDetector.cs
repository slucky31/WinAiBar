namespace WinAIBar.Core.Infrastructure;

public interface IIdleDetector
{
    TimeSpan GetIdleTime();
}
