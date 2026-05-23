using System.Text.Json;

namespace WinAIBar.Core.Services.Anthropic.Dto;

public sealed record AnthropicUsageResponse
{
    public required JsonElement RawData { get; init; }
    public required Dictionary<string, AnthropicQuotaDto> Quotas { get; init; }
}
