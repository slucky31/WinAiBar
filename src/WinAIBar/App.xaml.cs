using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Serilog;
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
            Log.Fatal(ex, "Fatal startup error");
            await Log.CloseAndFlushAsync().ConfigureAwait(false);
            Current.Exit();
            return;
        }

        _mainWindow = AppHost.Current.Services.GetRequiredService<MainWindow>();
        _mainWindow.Activate();
    }
}
