namespace WinAIBar.Core.Services.GitHub;

public sealed record GitHubOAuthOptions
{
    public string ClientId { get; init; } = "Iv1.b507a08c87ecfe98";
    public string DeviceCodeUrl { get; init; } = "https://github.com/login/device/code";
    public string TokenUrl { get; init; } = "https://github.com/login/oauth/access_token";
    public string Scope { get; init; } = "read:email";
}
