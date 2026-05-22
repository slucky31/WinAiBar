namespace WinAIBar.Core.Models;

public record class ProviderHealth(
    ProviderId Provider,
    ProviderStatus Status,
    DateTimeOffset CheckedAt,
    int? LastHttpStatus,
    string? LastError,
    TimeSpan? Latency);
