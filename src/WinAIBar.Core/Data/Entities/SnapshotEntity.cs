using WinAIBar.Core.Models;

namespace WinAIBar.Core.Data.Entities;

public class SnapshotEntity
{
    public int Id { get; set; }
    public ProviderId Provider { get; set; }
    public DateTimeOffset CapturedAt { get; set; }
    public string? RawPayload { get; set; }
    public List<QuotaEntity> Quotas { get; set; } = [];
}
