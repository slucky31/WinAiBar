using Microsoft.UI.Xaml;
using WinAIBar.Infrastructure;

namespace WinAIBar;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Closed += OnClosed;
    }

    private async void OnClosed(object sender, WindowEventArgs args)
    {
        await AppHost.StopAsync();
    }
}
