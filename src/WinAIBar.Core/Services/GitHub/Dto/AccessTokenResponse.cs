using System.Text.Json.Serialization;

namespace WinAIBar.Core.Services.GitHub.Dto;

public sealed record AccessTokenResponse(
    [property: JsonPropertyName("access_token")] string? AccessToken,
    [property: JsonPropertyName("token_type")] string? TokenType,
    [property: JsonPropertyName("scope")] string? Scope,
    [property: JsonPropertyName("error")] string? Error);
