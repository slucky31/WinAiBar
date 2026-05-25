using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using WinAIBar.Core.Infrastructure;
using WinAIBar.Core.Models;
using WinAIBar.Core.Services.Anthropic;
using WinAIBar.Core.Services.GitHub;

namespace WinAIBar.ViewModels;

public sealed class QuotaViewModel
{
    public string Label { get; init; } = string.Empty;
    public double Value { get; init; }
    public DateTimeOffset? ResetsAt { get; init; }
    public string? Subtitle { get; init; }
    public string ResetText { get; init; } = string.Empty;
}

public sealed partial class DashboardViewModel : ObservableObject
{
    private readonly IClaudeStateService _claudeState;
    private readonly ICopilotStateService _copilotState;
    private readonly DispatcherQueue _dispatcher;

    [ObservableProperty] private ProviderSnapshot? _claudeSnapshot;
    [ObservableProperty] private ProviderSnapshot? _copilotSnapshot;
    [ObservableProperty] private DateTimeOffset? _lastRefreshedAt;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RefreshCommand))]
    private bool _isRefreshEnabled = true;

    [ObservableProperty] private string _claudeStatusText = ProviderStatus.Unknown.ToString();
    [ObservableProperty] private string _copilotStatusText = ProviderStatus.Unknown.ToString();
    [ObservableProperty] private string _lastRefreshedText = "—";
    [ObservableProperty] private Visibility _copilotEmptyStateVisibility = Visibility.Visible;

    public ObservableCollection<QuotaViewModel> ClaudeMajorQuotas { get; } = new();
    public ObservableCollection<QuotaViewModel> ClaudeOtherQuotas { get; } = new();
    public ObservableCollection<QuotaViewModel> CopilotMajorQuotas { get; } = new();

    public DashboardViewModel(IClaudeStateService claudeState, ICopilotStateService copilotState)
    {
        _claudeState = claudeState;
        _copilotState = copilotState;
        _dispatcher = DispatcherQueue.GetForCurrentThread();

        if (claudeState.LatestSnapshot is { } cs)
            ApplyClaudeSnapshot(cs);
        if (copilotState.LatestSnapshot is { } cps)
            ApplyCopilotSnapshot(cps);

        if (claudeState.LatestHealth is { } ch)
            ClaudeStatusText = ch.Status.ToString();
        if (copilotState.LatestHealth is { } cph)
            CopilotStatusText = cph.Status.ToString();

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

    private void ApplyClaudeSnapshot(ProviderSnapshot snapshot)
    {
        ClaudeSnapshot = snapshot;
        LastRefreshedAt = DateTimeOffset.Now;
        LastRefreshedText = $"Last update: {DateTimeOffset.Now:HH:mm}";

        var all = snapshot.Quotas.OrderByDescending(q => q.Utilization).ToList();
        ReplaceItems(ClaudeMajorQuotas, all.Take(3).Select(ToGaugeViewModel));
        ReplaceItems(ClaudeOtherQuotas, all.Skip(3).Select(ToGaugeViewModel));

        if (_claudeState.LatestHealth is { } health)
            ClaudeStatusText = health.Status.ToString();
    }

    private void ApplyCopilotSnapshot(ProviderSnapshot snapshot)
    {
        CopilotSnapshot = snapshot;
        LastRefreshedAt = DateTimeOffset.Now;
        LastRefreshedText = $"Last update: {DateTimeOffset.Now:HH:mm}";
        CopilotEmptyStateVisibility = Visibility.Collapsed;

        var major = snapshot.Quotas
            .OrderByDescending(q => q.Utilization)
            .Take(2)
            .ToList();

        ReplaceItems(CopilotMajorQuotas, major.Select(ToGaugeViewModel));

        if (_copilotState.LatestHealth is { } health)
            CopilotStatusText = health.Status.ToString();
    }

    private static QuotaViewModel ToGaugeViewModel(UsageQuota q)
    {
        string? subtitle = null;
        if (q.Used.HasValue && q.Limit.HasValue)
            subtitle = q.Unit is not null
                ? $"{q.Used:N0} / {q.Limit:N0} {q.Unit}"
                : $"{q.Used:N0} / {q.Limit:N0}";

        return new QuotaViewModel
        {
            Label = q.Label,
            Value = q.Utilization,
            ResetsAt = q.ResetsAt,
            Subtitle = subtitle,
            ResetText = TimeFormatHelper.FormatReset(q.ResetsAt),
        };
    }

    private static void ReplaceItems<T>(ObservableCollection<T> collection, IEnumerable<T> items)
    {
        collection.Clear();
        foreach (var item in items)
            collection.Add(item);
    }
}
