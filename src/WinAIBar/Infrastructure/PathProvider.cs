using System;
using System.IO;

namespace WinAIBar.Infrastructure;

public static class PathProvider
{
    public static string LocalAppData { get; } = EnsureDirectory(
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WinAIBar"));

    public static string LogsDirectory { get; } = EnsureDirectory(Path.Combine(LocalAppData, "logs"));

    public static string DataDirectory { get; } = EnsureDirectory(Path.Combine(LocalAppData, "data"));

    private static string EnsureDirectory(string path)
    {
        Directory.CreateDirectory(path);
        return path;
    }
}
