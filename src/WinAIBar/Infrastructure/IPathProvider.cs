namespace WinAIBar.Infrastructure;

public interface IPathProvider
{
    string LocalAppData { get; }
    string LogsDirectory { get; }
    string DataDirectory { get; }
}
