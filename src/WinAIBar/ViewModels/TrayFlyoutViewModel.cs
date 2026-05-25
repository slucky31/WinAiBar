using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using WinAIBar.Core.Models;
using WinAIBar.Core.Services.Anthropic;
using WinAIBar.Core.Services.GitHub;

namespace WinAIBar.ViewModels;

public sealed partial class TrayFlyoutViewModel : ObservableObject
{
    private readonly MainWindow _mainWindow;
    private readonly DispatcherQueue _dispatcher;

    [ObservableProperty] private string _claudeSessionLabel = "Session 5h";
    [ObservableProperty] private double _claudeSessionValue;
    [ObservableProperty] private string _claudeSessionResetText = string.Empty;

    [ObservableProperty] private string _claudeWeeklyLabel = "Weekly · All";
    [ObservableProperty] private double _claudeWeeklyValue;
    [ObservableProperty] private string _claudeWeeklyResetText = string.Empty;

    [ObservableProperty] private string _copilotPremiumLabel = "Premium requests";
    [ObservableProperty] private double _copilotPremiumValue;
    [ObservableProperty] private string _copilotPremiumResetText = string.Empty;

    [ObservableProperty] private string _copilotCreditsLabel = "Credits";
    [ObservableProperty] private double _copilotCreditsValue;
    [ObservableProperty] private string _copilotCreditsResetText = string.Empty;

    [ObservableProperty] private string _lastUpdateText = "—";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RefreshCommand))]
    private bool _isRefreshEnabled = true;

    public TrayFlyoutViewModel(
        IClaudeStateService claudeState,
        ICopilotStateService copilotState,
        MainWindow mainWindow)
    {
        _mainWindow = mainWindow;
        _dispatcher = DispatcherQueue.GetForCurrentThread();

        if (claudeState.LatestSnapshot is { } cs)
            ApplyClaudeSnapshot(cs);
        if (copilotState.LatestSnapshot is { } cps)
            ApplyCopilotSnapshot(cps);

        WeakReferenceMessenger.Default.Register<ProviderSnapshot>(this, (_, msg) =>
        {
            if (msg.Provider == ProviderId.Claude)
                _dispatcher.TryEnqueue(() => ApplyClaudeSnapshot(msg));
            else if (msg.Provider == ProviderId.Copilot)
                _dispatcher.TryEnqueue(() => ApplyCopilotSnapshot(msg));
        });
    }

    private bool CanRefresh() => IsRefreshEnabled;

    [RelayCommand(CanExecute = nameof(CanRefresh))]
    private void Refresh()
    {
        IsRefreshEnabled = false;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
        timer.Tick += (_, _) => { timer.Stop(); IsRefreshEnabled = true; };
        timer.Start();
    }

    [RelayCommand]
    private void OpenSettings() => _dispatcher.TryEnqueue(_mainWindow.Activate);

    private void ApplyClaudeSnapshot(ProviderSnapshot snapshot)
    {
        var session = FindQuota(snapshot, "session");
        var weekly = FindQuota(snapshot, "weekly");

        if (session is not null)
        {
            ClaudeSessionLabel = session.Label;
            ClaudeSessionValue = session.Utilization;
            ClaudeSessionResetText = FormatReset(session.ResetsAt);
        }

        if (weekly is not null)
        {
            ClaudeWeeklyLabel = weekly.Label;
            ClaudeWeeklyValue = weekly.Utilization;
            ClaudeWeeklyResetText = FormatReset(weekly.ResetsAt);
        }

        LastUpdateText = $"Last update: {DateTimeOffset.Now:HH:mm}";
    }

    private void ApplyCopilotSnapshot(ProviderSnapshot snapshot)
    {
        var premium = FindQuota(snapshot, "premium");
        var credits = FindQuota(snapshot, "credit");

        if (premium is not null)
        {
            CopilotPremiumLabel = premium.Label;
            CopilotPremiumValue = premium.Utilization;
            CopilotPremiumResetText = FormatReset(premium.ResetsAt);
        }

        if (credits is not null)
        {
            CopilotCreditsLabel = credits.Label;
            CopilotCreditsValue = credits.Utilization;
            CopilotCreditsResetText = FormatReset(credits.ResetsAt);
        }

        LastUpdateText = $"Last update: {DateTimeOffset.Now:HH:mm}";
    }

    private static UsageQuota? FindQuota(ProviderSnapshot snapshot, string keyPart) =>
        snapshot.Quotas.FirstOrDefault(q =>
            q.Key.Contains(keyPart, StringComparison.OrdinalIgnoreCase));

    private static string FormatReset(DateTimeOffset? resetsAt)
    {
        if (resetsAt is null) return string.Empty;
        var remaining = resetsAt.Value - DateTimeOffset.UtcNow;
        if (remaining <= TimeSpan.Zero) return "expired";
        return remaining.TotalDays >= 1
            ? $"resets in {(int)remaining.TotalDays}d {remaining.Hours}h"
            : remaining.TotalHours >= 1
                ? $"resets in {(int)remaining.TotalHours}h {remaining.Minutes:D2}m"
                : $"resets in {remaining.Minutes}m";
    }
}
