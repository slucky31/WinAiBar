using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using H.NotifyIcon;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using System.Drawing;
using System.Runtime.Versioning;
using WinAIBar;
using WinAIBar.Core.Models;
using WinAIBar.Views.Flyouts;

namespace WinAIBar.Services.Tray;

[SupportedOSPlatform("windows10.0.17763.0")]
public sealed partial class TrayController : IDisposable
{
    private readonly ITrayIconRenderer _renderer;
    private readonly MainWindow _mainWindow;
    private readonly TrayFlyout _flyout;
    private readonly ILogger<TrayController> _logger;

    private TaskbarIcon? _trayIcon;
    private Icon? _currentIcon;
    private DispatcherQueue? _dispatcher;

    private double _claudeMax;
    private double _copilotMax;
    private DateTimeOffset? _claudeResetsAt;

    public TrayController(
        ITrayIconRenderer renderer,
        MainWindow mainWindow,
        TrayFlyout flyout,
        ILogger<TrayController> logger)
    {
        _renderer = renderer;
        _mainWindow = mainWindow;
        _flyout = flyout;
        _logger = logger;
    }

    public void Start()
    {
        _dispatcher = DispatcherQueue.GetForCurrentThread();

        _currentIcon = _renderer.Render(0.0);

        _trayIcon = new TaskbarIcon
        {
            ToolTipText = "WinAIBar",
            Icon = _currentIcon,
        };
        _trayIcon.ContextMenuMode = ContextMenuMode.SecondWindow;
        _trayIcon.ContextFlyout = BuildContextMenu();
        _trayIcon.LeftClickCommand = new RelayCommand(ShowTrayFlyout);
        _trayIcon.ForceCreate(true);

        WeakReferenceMessenger.Default.Register<ProviderSnapshot>(this, (_, msg) =>
        {
            if (msg.Provider == ProviderId.Claude)
                HandleClaudeSnapshot(msg);
            else if (msg.Provider == ProviderId.Copilot)
                HandleCopilotSnapshot(msg);
        });

        LogStarted(_logger);
    }

    private MenuFlyout BuildContextMenu()
    {
        var openItem = new MenuFlyoutItem { Text = "Open dashboard" };
        openItem.Click += (_, _) => ShowMainWindow();

        // will be wired to polling services in a future prompt
        var refreshItem = new MenuFlyoutItem { Text = "Refresh now", IsEnabled = false };

        var settingsItem = new MenuFlyoutItem { Text = "Settings" };
        settingsItem.Click += (_, _) => ShowMainWindow();

        var exitItem = new MenuFlyoutItem { Text = "Exit" };
        exitItem.Click += (_, _) => _dispatcher?.TryEnqueue(_mainWindow.Close);

        var menu = new MenuFlyout();
        menu.Items.Add(openItem);
        menu.Items.Add(refreshItem);
        menu.Items.Add(new MenuFlyoutSeparator());
        menu.Items.Add(settingsItem);
        menu.Items.Add(new MenuFlyoutSeparator());
        menu.Items.Add(exitItem);
        return menu;
    }

    private void HandleClaudeSnapshot(ProviderSnapshot snapshot)
    {
        _claudeMax = GetCriticalMax(snapshot, "session", "weekly");
        _claudeResetsAt = snapshot.Quotas
            .FirstOrDefault(q => q.Key.Contains("session", StringComparison.OrdinalIgnoreCase))
            ?.ResetsAt;
        RefreshIcon();
    }

    private void HandleCopilotSnapshot(ProviderSnapshot snapshot)
    {
        _copilotMax = GetCriticalMax(snapshot, "premium", "credits");
        RefreshIcon();
    }

    private static double GetCriticalMax(ProviderSnapshot snapshot, params string[] keyParts)
    {
        var critical = snapshot.Quotas
            .Where(q => keyParts.Any(k => q.Key.Contains(k, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        return critical.Count > 0
            ? critical.Select(q => q.Utilization).Max()
            : snapshot.Quotas.Select(q => q.Utilization).DefaultIfEmpty(0.0).Max();
    }

    private void RefreshIcon()
    {
        var max = Math.Max(_claudeMax, _copilotMax);
        _dispatcher?.TryEnqueue(() =>
        {
            if (_trayIcon is null) return;
            var oldIcon = _currentIcon;
            _currentIcon = _renderer.Render(max);
            _trayIcon.Icon = _currentIcon;
            _trayIcon.ToolTipText = BuildTooltip();
            oldIcon?.Dispose();
        });
    }

    private string BuildTooltip()
    {
        var parts = new List<string>();

        if (_claudeMax > 0)
            parts.Add($"Claude session {_claudeMax:P0}");
        if (_copilotMax > 0)
            parts.Add($"Copilot {_copilotMax:P0}");

        if (_claudeResetsAt is { } resetsAt)
        {
            var remaining = resetsAt - DateTimeOffset.UtcNow;
            if (remaining > TimeSpan.Zero)
                parts.Add($"resets in {FormatDuration(remaining)}");
        }

        return parts.Count > 0 ? string.Join(" · ", parts) : "WinAIBar";
    }

    private static string FormatDuration(TimeSpan span) =>
        span.TotalHours >= 1
            ? $"{(int)span.TotalHours}h {span.Minutes:D2}m"
            : $"{span.Minutes}m";

    private void ShowTrayFlyout() =>
        _dispatcher?.TryEnqueue(_flyout.ShowAtTaskbar);

    private void ShowMainWindow() =>
        _dispatcher?.TryEnqueue(_mainWindow.Activate);

    public void Dispose()
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
        // TaskbarIcon is a UI object — dispose on the UI thread
        _dispatcher?.TryEnqueue(() =>
        {
            _trayIcon?.Dispose();
            _trayIcon = null;
            _currentIcon?.Dispose();
            _currentIcon = null;
        });
    }

    [LoggerMessage(EventId = 100, Level = LogLevel.Information, Message = "TrayController started")]
    private static partial void LogStarted(ILogger logger);
}
