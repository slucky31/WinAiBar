namespace WinAIBar.Core.Services.Anthropic;

public sealed class AnthropicUnauthorizedException : Exception
{
    public AnthropicUnauthorizedException(string message) : base(message) { }
    public AnthropicUnauthorizedException(string message, Exception inner) : base(message, inner) { }
}
