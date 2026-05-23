namespace WinAIBar.Core.Data.Entities;

public class QuotaEntity
{
    public int Id { get; set; }
    public int SnapshotId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public double Utilization { get; set; }
    public DateTimeOffset? ResetsAt { get; set; }
    public long? Used { get; set; }
    public long? Limit { get; set; }
    public string? Unit { get; set; }
    public string? Model { get; set; }
}
