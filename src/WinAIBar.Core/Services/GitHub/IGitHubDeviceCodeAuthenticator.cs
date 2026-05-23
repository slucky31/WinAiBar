using WinAIBar.Core.Services.GitHub.Dto;

namespace WinAIBar.Core.Services.GitHub;

public interface IGitHubDeviceCodeAuthenticator
{
    Task<DeviceCodeResponse> StartAsync(CancellationToken ct);
    Task<string> PollForTokenAsync(DeviceCodeResponse start, IProgress<string>? progress, CancellationToken ct);
}
