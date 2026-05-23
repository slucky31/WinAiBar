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
        UnhandledException += OnWinUIUnhandledException;
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            await AppHost.StartAsync();
            _mainWindow = AppHost.Current.Services.GetRequiredService<MainWindow>();
            _mainWindow.Activate();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Fatal startup error");
            await Log.CloseAndFlushAsync().ConfigureAwait(false);
            Current.Exit();
        }
    }

    private static void OnWinUIUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        Log.Fatal(e.Exception, "Unhandled WinUI exception: {Message}", e.Message);
        Log.CloseAndFlush();
        e.Handled = true;
    }
}
