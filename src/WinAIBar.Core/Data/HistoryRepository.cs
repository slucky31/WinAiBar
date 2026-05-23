using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WinAIBar.Core.Data.Abstractions;
using WinAIBar.Core.Data.Entities;
using WinAIBar.Core.Models;

namespace WinAIBar.Core.Data;

public partial class HistoryRepository(WinAIBarDbContext context, ILogger<HistoryRepository> logger)
    : IHistoryRepository
{
    public async Task SaveAsync(ProviderSnapshot snapshot, CancellationToken ct)
    {
        LogSaving(logger, snapshot.Provider);
        var entity = new SnapshotEntity
        {
            Provider = snapshot.Provider,
            CapturedAt = snapshot.CapturedAt,
            RawPayload = snapshot.RawPayload,
            Quotas = snapshot.Quotas.Select(q => new QuotaEntity
            {
                Key = q.Key,
                Label = q.Label,
                Utilization = q.Utilization,
                ResetsAt = q.ResetsAt,
                Used = q.Used,
                Limit = q.Limit,
                Unit = q.Unit,
                Model = q.Model
            }).ToList()
        };
        context.Snapshots.Add(entity);
        await context.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ProviderSnapshot>> GetRecentAsync(
        ProviderId provider, int count, CancellationToken ct)
    {
        var entities = await context.Snapshots
            .Where(s => s.Provider == provider)
            .OrderByDescending(s => s.CapturedAt)
            .Take(count)
            .Include(s => s.Quotas)
            .AsNoTracking()
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return entities.Select(ToSnapshot).ToList();
    }

    public async Task<IReadOnlyList<ProviderSnapshot>> GetRangeAsync(
        ProviderId provider, DateTimeOffset rangeStart, DateTimeOffset rangeEnd, CancellationToken ct)
    {
        var entities = await context.Snapshots
            .Where(s => s.Provider == provider && s.CapturedAt >= rangeStart && s.CapturedAt <= rangeEnd)
            .OrderByDescending(s => s.CapturedAt)
            .Include(s => s.Quotas)
            .AsNoTracking()
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return entities.Select(ToSnapshot).ToList();
    }

    public async Task PurgeOlderThanAsync(DateTimeOffset cutoff, CancellationToken ct)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

        await context.Quotas
            .Where(q => context.Snapshots
                .Where(s => s.CapturedAt < cutoff)
                .Select(s => s.Id)
                .Contains(q.SnapshotId))
            .ExecuteDeleteAsync(ct)
            .ConfigureAwait(false);

        var count = await context.Snapshots
            .Where(s => s.CapturedAt < cutoff)
            .ExecuteDeleteAsync(ct)
            .ConfigureAwait(false);

        await transaction.CommitAsync(ct).ConfigureAwait(false);

        if (count > 0)
            LogPurged(logger, count, cutoff);
    }

    private static ProviderSnapshot ToSnapshot(SnapshotEntity entity)
    {
        var quotas = entity.Quotas
            .Select(q => new UsageQuota(q.Key, q.Label, q.Utilization, q.ResetsAt, q.Used, q.Limit, q.Unit, q.Model))
            .ToList();
        return new ProviderSnapshot(entity.Provider, entity.CapturedAt, quotas, entity.RawPayload);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Saving snapshot for {Provider}")]
    private static partial void LogSaving(ILogger logger, ProviderId provider);

    [LoggerMessage(Level = LogLevel.Information, Message = "Purged {Count} snapshots older than {Cutoff}")]
    private static partial void LogPurged(ILogger logger, int count, DateTimeOffset cutoff);
}
