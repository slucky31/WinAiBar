using Microsoft.EntityFrameworkCore;
using WinAIBar.Core.Data.Entities;

namespace WinAIBar.Core.Data;

public class WinAIBarDbContext : DbContext
{
    public WinAIBarDbContext(DbContextOptions<WinAIBarDbContext> options) : base(options) { }

    public DbSet<SnapshotEntity> Snapshots => Set<SnapshotEntity>();
    public DbSet<QuotaEntity> Quotas => Set<QuotaEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
        => modelBuilder.ApplyConfigurationsFromAssembly(typeof(WinAIBarDbContext).Assembly);
}
