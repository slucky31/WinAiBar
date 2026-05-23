using WinAIBar.Core.Models;

namespace WinAIBar.Core.Data.Abstractions;

public interface IHistoryRepository
{
    Task SaveAsync(ProviderSnapshot snapshot, CancellationToken ct);
    Task<IReadOnlyList<ProviderSnapshot>> GetRecentAsync(ProviderId provider, int count, CancellationToken ct);
    Task<IReadOnlyList<ProviderSnapshot>> GetRangeAsync(ProviderId provider, DateTimeOffset rangeStart, DateTimeOffset rangeEnd, CancellationToken ct);
    Task PurgeOlderThanAsync(DateTimeOffset cutoff, CancellationToken ct);
}
