using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using WinAIBar.Infrastructure;
using WinAIBar.Views;

namespace WinAIBar;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Content = AppHost.Current.Services.GetRequiredService<Shell>();
        Closed += OnClosed;
    }

    private async void OnClosed(object sender, WindowEventArgs args)
    {
        await AppHost.StopAsync();
    }
}
