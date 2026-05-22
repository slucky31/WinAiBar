using System;
using System.IO;

namespace WinAIBar.Infrastructure;

public sealed class PathProvider : IPathProvider
{
    public static readonly PathProvider Instance = new();

    public string LocalAppData { get; }
    public string LogsDirectory { get; }
    public string DataDirectory { get; }

    private PathProvider()
    {
        LocalAppData = EnsureDirectory(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WinAIBar"));
        LogsDirectory = EnsureDirectory(Path.Combine(LocalAppData, "logs"));
        DataDirectory = EnsureDirectory(Path.Combine(LocalAppData, "data"));
    }

    private static string EnsureDirectory(string path)
    {
        Directory.CreateDirectory(path);
        return path;
    }
}
