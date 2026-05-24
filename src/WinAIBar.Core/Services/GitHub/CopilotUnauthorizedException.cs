namespace WinAIBar.Core.Services.GitHub;

public sealed class CopilotUnauthorizedException : Exception
{
    public CopilotUnauthorizedException(string message) : base(message) { }
    public CopilotUnauthorizedException(string message, Exception inner) : base(message, inner) { }
}
