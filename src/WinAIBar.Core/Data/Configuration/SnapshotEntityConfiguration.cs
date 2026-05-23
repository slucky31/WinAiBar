using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WinAIBar.Core.Data.Entities;

namespace WinAIBar.Core.Data.Configuration;

internal sealed class SnapshotEntityConfiguration : IEntityTypeConfiguration<SnapshotEntity>
{
    public void Configure(EntityTypeBuilder<SnapshotEntity> builder)
    {
        // SQLite does not support ORDER BY on DateTimeOffset as TEXT; store as Unix ms (long).
        builder.Property(s => s.CapturedAt)
            .HasConversion(
                v => v.ToUnixTimeMilliseconds(),
                v => DateTimeOffset.FromUnixTimeMilliseconds(v));

        builder.HasIndex(s => new { s.Provider, s.CapturedAt })
            .IsDescending(false, true);

        builder.HasMany(s => s.Quotas)
            .WithOne()
            .HasForeignKey(q => q.SnapshotId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
