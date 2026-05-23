using System.Runtime.InteropServices;
using WinAIBar.Core.Infrastructure;

namespace WinAIBar.Infrastructure;

public sealed class IdleDetector : IIdleDetector
{
    public TimeSpan GetIdleTime()
    {
        var info = new LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>() };
        if (!GetLastInputInfo(ref info))
            return TimeSpan.Zero;

        // TickCount wraps every ~49.7 days; subtraction handles wraparound correctly via int arithmetic
        var idleMs = Environment.TickCount - (int)info.dwTime;
        return idleMs > 0 ? TimeSpan.FromMilliseconds(idleMs) : TimeSpan.Zero;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

    [StructLayout(LayoutKind.Sequential)]
    private struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }
}
