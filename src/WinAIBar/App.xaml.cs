using Microsoft.UI.Xaml;
using WinAIBar.Infrastructure;

namespace WinAIBar;

public partial class App : Application
{
    private MainWindow? _mainWindow;

    public App()
    {
        InitializeComponent();
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        await AppHost.StartAsync();

        _mainWindow = new MainWindow();
        _mainWindow.Activate();
    }
}
