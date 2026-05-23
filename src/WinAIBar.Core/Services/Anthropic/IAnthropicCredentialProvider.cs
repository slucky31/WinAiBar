namespace WinAIBar.Core.Services.Anthropic;

public interface IAnthropicCredentialProvider
{
    Task<AnthropicCredentials?> GetAsync(CancellationToken ct = default);
    bool IsAvailable();
}
