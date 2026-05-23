namespace WinAIBar.Core.Services.GitHub;

public sealed class DeviceCodeAccessDeniedException : Exception
{
    public DeviceCodeAccessDeniedException(string message) : base(message) { }
    public DeviceCodeAccessDeniedException(string message, Exception inner) : base(message, inner) { }
}
