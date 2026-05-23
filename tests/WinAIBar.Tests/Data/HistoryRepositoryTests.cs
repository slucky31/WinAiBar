using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using WinAIBar.Core.Data;
using WinAIBar.Core.Models;
using Xunit;

namespace WinAIBar.Tests.Data;

public sealed class HistoryRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly WinAIBarDbContext _context;

    public HistoryRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<WinAIBarDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new WinAIBarDbContext(options);
        _context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private HistoryRepository CreateRepo() =>
        new(_context, NullLogger<HistoryRepository>.Instance);

    [Fact]
    public async Task SavePersistsSnapshotWithQuotas()
    {
        var ct = TestContext.Current.CancellationToken;
        var repo = CreateRepo();
        var snapshot = new ProviderSnapshot(
            ProviderId.Claude,
            DateTimeOffset.UtcNow,
            [new UsageQuota("session-5h", "Session 5h", 0.65, null, 6500, 10000, "tokens", "sonnet")],
            """{"raw":"payload"}""");

        await repo.SaveAsync(snapshot, ct);

        Assert.Equal(1, await _context.Snapshots.CountAsync(ct));
        Assert.Equal(1, await _context.Quotas.CountAsync(ct));
    }

    [Fact]
    public async Task GetRecentReturnsCountMostRecentForProvider()
    {
        var ct = TestContext.Current.CancellationToken;
        var repo = CreateRepo();
        var now = DateTimeOffset.UtcNow;

        for (var i = 0; i < 5; i++)
        {
            await repo.SaveAsync(
                new ProviderSnapshot(ProviderId.Claude, now.AddMinutes(-i), [], null),
                ct);
        }

        await repo.SaveAsync(
            new ProviderSnapshot(ProviderId.Copilot, now, [], null),
            ct);

        var recent = await repo.GetRecentAsync(ProviderId.Claude, 3, ct);

        Assert.Equal(3, recent.Count);
        Assert.All(recent, s => Assert.Equal(ProviderId.Claude, s.Provider));
    }

    [Fact]
    public async Task GetRangeReturnsSnapshotsWithinWindow()
    {
        var ct = TestContext.Current.CancellationToken;
        var repo = CreateRepo();
        var now = DateTimeOffset.UtcNow;

        await repo.SaveAsync(new ProviderSnapshot(ProviderId.Claude, now.AddHours(-3), [], null), ct);
        await repo.SaveAsync(new ProviderSnapshot(ProviderId.Claude, now.AddHours(-1), [], null), ct);
        await repo.SaveAsync(new ProviderSnapshot(ProviderId.Claude, now, [], null), ct);

        var results = await repo.GetRangeAsync(ProviderId.Claude, now.AddHours(-2), now, ct);

        Assert.Equal(2, results.Count);
        Assert.All(results, s => Assert.True(s.CapturedAt >= now.AddHours(-2)));
    }

    [Fact]
    public async Task GetRangeReturnsEmptyWhenNoMatchingSnapshots()
    {
        var ct = TestContext.Current.CancellationToken;
        var repo = CreateRepo();
        var now = DateTimeOffset.UtcNow;

        await repo.SaveAsync(new ProviderSnapshot(ProviderId.Claude, now.AddDays(-5), [], null), ct);

        var results = await repo.GetRangeAsync(ProviderId.Claude, now.AddHours(-1), now, ct);

        Assert.Empty(results);
    }

    [Fact]
    public async Task PurgeOlderThanRemovesSnapshotsAndQuotas()
    {
        var ct = TestContext.Current.CancellationToken;
        var repo = CreateRepo();
        var now = DateTimeOffset.UtcNow;

        await repo.SaveAsync(new ProviderSnapshot(
            ProviderId.Claude, now.AddDays(-2),
            [new UsageQuota("k", "L", 0.5, null, null, null, null, null)],
            null), ct);

        await repo.SaveAsync(new ProviderSnapshot(
            ProviderId.Claude, now.AddHours(-12),
            [new UsageQuota("k", "L", 0.3, null, null, null, null, null)],
            null), ct);

        await repo.PurgeOlderThanAsync(now.AddDays(-1), ct);

        Assert.Equal(1, await _context.Snapshots.CountAsync(ct));
        Assert.Equal(1, await _context.Quotas.CountAsync(ct));
    }
}
