using Microsoft.UI.Xaml;
using WinAIBar.Infrastructure;
using WinAIBar.Views;

namespace WinAIBar;

public sealed partial class MainWindow : Window
{
    public MainWindow(Shell shell)
    {
        ArgumentNullException.ThrowIfNull(shell);
        InitializeComponent();
        Content = shell;
        Closed += OnClosed;
    }

    private async void OnClosed(object sender, WindowEventArgs args)
    {
        await AppHost.StopAsync();
    }
}
