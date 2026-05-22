namespace WinAIBar.Core.Models;

public record class UsageQuota(
    string Key,
    string Label,
    double Utilization,
    DateTimeOffset? ResetsAt,
    long? Used,
    long? Limit,
    string? Unit,
    string? Model);
