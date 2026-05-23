namespace WinAIBar.Core.Services.Anthropic;

public sealed record AnthropicCredentials(
    string AccessToken,
    string? RefreshToken,
    DateTimeOffset? ExpiresAt);
