using Microsoft.EntityFrameworkCore;
using WinAIBar.Core.Data.Entities;

namespace WinAIBar.Core.Data;

public class WinAIBarDbContext : DbContext
{
    public WinAIBarDbContext(DbContextOptions<WinAIBarDbContext> options) : base(options) { }

    public DbSet<SnapshotEntity> Snapshots => Set<SnapshotEntity>();
    public DbSet<QuotaEntity> Quotas => Set<QuotaEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // SQLite does not support ORDER BY on DateTimeOffset as TEXT; store as Unix ms (long).
        modelBuilder.Entity<SnapshotEntity>()
            .Property(s => s.CapturedAt)
            .HasConversion(
                v => v.ToUnixTimeMilliseconds(),
                v => DateTimeOffset.FromUnixTimeMilliseconds(v));

        modelBuilder.Entity<QuotaEntity>()
            .Property(q => q.ResetsAt)
            .HasConversion(
                v => v.HasValue ? (long?)v.Value.ToUnixTimeMilliseconds() : null,
                v => v.HasValue ? (DateTimeOffset?)DateTimeOffset.FromUnixTimeMilliseconds(v.Value) : null);

        modelBuilder.Entity<SnapshotEntity>()
            .HasIndex(s => new { s.Provider, s.CapturedAt })
            .IsDescending(false, true);

        modelBuilder.Entity<SnapshotEntity>()
            .HasMany(s => s.Quotas)
            .WithOne()
            .HasForeignKey(q => q.SnapshotId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
