using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Serilog;
using WinAIBar.Core.Models;
using WinAIBar.Infrastructure;
using WinAIBar.Services.Tray;

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

            var trayController = AppHost.Current.Services.GetRequiredService<TrayController>();
            trayController.Start();

            InjectDebugFakeQuota();

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

    private static void InjectDebugFakeQuota()
    {
        var cmdArgs = Environment.GetCommandLineArgs();
        var arg = Array.Find(cmdArgs, a =>
            a.StartsWith("--debug-fake-quota=", StringComparison.OrdinalIgnoreCase));

        if (arg is null) return;

        if (!double.TryParse(
                arg.AsSpan("--debug-fake-quota=".Length),
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture,
                out var util))
            return;

        var snapshot = new ProviderSnapshot(
            ProviderId.Claude,
            DateTimeOffset.UtcNow,
            [new UsageQuota("session-5h", "Session 5h", util, DateTimeOffset.UtcNow.AddHours(4), null, null, "tokens", null)],
            null);
        WeakReferenceMessenger.Default.Send(snapshot);
    }

    private static void OnWinUIUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        Log.Fatal(e.Exception, "Unhandled WinUI exception: {Message}", e.Message);
        Log.CloseAndFlush();
        e.Handled = true;
    }
}
