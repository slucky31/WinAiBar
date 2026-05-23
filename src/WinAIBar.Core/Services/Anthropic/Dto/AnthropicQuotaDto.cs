namespace WinAIBar.Core.Services.Anthropic.Dto;

public sealed record AnthropicQuotaDto
{
    public required double Utilization { get; init; }
    public DateTimeOffset? ResetsAt { get; init; }
    public long? Used { get; init; }
    public long? Limit { get; init; }
    public string? Label { get; init; }
}
