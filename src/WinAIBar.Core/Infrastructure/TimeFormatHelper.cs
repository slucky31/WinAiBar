namespace WinAIBar.Core.Infrastructure;

public static class TimeFormatHelper
{
    public static string FormatReset(DateTimeOffset? resetsAt, DateTimeOffset? now = null)
    {
        if (resetsAt is null) return string.Empty;
        var remaining = resetsAt.Value - (now ?? DateTimeOffset.UtcNow);
        if (remaining <= TimeSpan.Zero) return "expired";
        return remaining.TotalDays >= 1
            ? $"resets in {(int)remaining.TotalDays}d {remaining.Hours}h"
            : remaining.TotalHours >= 1
                ? $"resets in {(int)remaining.TotalHours}h {remaining.Minutes:D2}m"
                : $"resets in {remaining.Minutes}m";
    }
}
