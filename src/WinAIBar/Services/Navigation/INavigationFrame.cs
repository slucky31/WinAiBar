using Microsoft.UI.Xaml.Controls;

namespace WinAIBar.Services.Navigation;

public interface INavigationFrame
{
    void Initialize(Frame contentFrame);
}
