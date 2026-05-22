using Microsoft.UI.Xaml.Controls;

namespace WinAIBar.Services.Navigation;

public interface INavigationService
{
    void Initialize(Frame contentFrame);
    void NavigateTo(string tag);
    bool CanGoBack { get; }
    void GoBack();
}
