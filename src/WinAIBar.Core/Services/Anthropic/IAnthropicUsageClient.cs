using WinAIBar.Core.Models;

namespace WinAIBar.Core.Services.Anthropic;

public interface IAnthropicUsageClient
{
    Task<ProviderSnapshot> FetchAsync(CancellationToken ct = default);
}
