namespace WinAIBar.Core.Services.Navigation;

public interface INavigationService
{
    void NavigateTo(string tag);
    bool CanGoBack { get; }
    void GoBack();
}
