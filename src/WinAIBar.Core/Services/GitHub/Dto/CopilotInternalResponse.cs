using System.Text.Json;

namespace WinAIBar.Core.Services.GitHub.Dto;

public sealed record CopilotInternalResponse
{
    public string? Login { get; init; }
    public int? ChatEnabled { get; init; }
    public long? AccessTypeSku { get; init; }
    public Dictionary<string, double>? Quotas { get; init; }
    public JsonElement RawData { get; init; }
}
