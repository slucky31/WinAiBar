namespace WinAIBar.Core.Services.GitHub;

public sealed class DeviceCodeExpiredException : Exception
{
    public DeviceCodeExpiredException(string message) : base(message) { }
    public DeviceCodeExpiredException(string message, Exception inner) : base(message, inner) { }
}
