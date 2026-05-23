using WinAIBar.Core.Models;

namespace WinAIBar.Core.Providers;

public interface IProviderHealthChecker
{
    ProviderId ProviderId { get; }
    Task<ProviderHealth> CheckHealthAsync(CancellationToken ct);
}
