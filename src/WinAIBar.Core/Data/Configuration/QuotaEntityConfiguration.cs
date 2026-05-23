using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WinAIBar.Core.Data.Entities;

namespace WinAIBar.Core.Data.Configuration;

internal sealed class QuotaEntityConfiguration : IEntityTypeConfiguration<QuotaEntity>
{
    public void Configure(EntityTypeBuilder<QuotaEntity> builder)
    {
        // SQLite does not support ORDER BY on DateTimeOffset as TEXT; store as Unix ms (long).
        builder.Property(q => q.ResetsAt)
            .HasConversion(
                v => v.HasValue ? (long?)v.Value.ToUnixTimeMilliseconds() : null,
                v => v.HasValue ? (DateTimeOffset?)DateTimeOffset.FromUnixTimeMilliseconds(v.Value) : null);
    }
}
