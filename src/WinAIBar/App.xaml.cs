using System;
using System.Diagnostics;
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
        try
        {
            await AppHost.StartAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Fatal startup error: {ex}");
            Current.Exit();
            return;
        }

        _mainWindow = new MainWindow();
        _mainWindow.Activate();
    }
}
