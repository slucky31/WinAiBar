using System.Text.Json;

namespace WinAIBar.Core.Services.GitHub.Dto;

public sealed record CopilotPremiumUsageResponse
{
    public required IReadOnlyList<CopilotPremiumUsageItem> UsageItems { get; init; }
    public required JsonElement RawData { get; init; }
}

public sealed record CopilotPremiumUsageItem
{
    public string? Key { get; init; }
    public string? Label { get; init; }
    public long? Used { get; init; }
    public long? Limit { get; init; }
    public string? Unit { get; init; }
    public DateTimeOffset? ResetsAt { get; init; }
    public double Utilization { get; init; }
}
