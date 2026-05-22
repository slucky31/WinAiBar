namespace WinAIBar.Core.Models;

public record class ProviderSnapshot(
    ProviderId Provider,
    DateTimeOffset CapturedAt,
    IReadOnlyList<UsageQuota> Quotas,
    string? RawPayload);
