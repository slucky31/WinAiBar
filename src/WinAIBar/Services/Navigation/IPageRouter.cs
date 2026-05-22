namespace WinAIBar.Services.Navigation;

internal interface IPageRouter
{
    Type? Resolve(string tag);
}
