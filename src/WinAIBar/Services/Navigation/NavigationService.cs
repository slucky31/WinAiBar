using Microsoft.UI.Xaml.Controls;
using WinAIBar.Views.Pages;

namespace WinAIBar.Services.Navigation;

public sealed class NavigationService : INavigationService
{
    private Frame? _frame;

    private static readonly Dictionary<string, Type> PageRegistry = new()
    {
        ["Dashboard"] = typeof(DashboardPage),
        ["Claude"]    = typeof(ClaudePage),
        ["Copilot"]   = typeof(CopilotPage),
        ["History"]   = typeof(HistoryPage),
        ["Health"]    = typeof(HealthPage),
        ["Cost"]      = typeof(CostPage),
        ["Settings"]  = typeof(SettingsPage),
    };

    public void Initialize(Frame contentFrame)
    {
        _frame = contentFrame;
        _frame.Navigate(typeof(DashboardPage));
    }

    public void NavigateTo(string tag)
    {
        if (_frame is not null && PageRegistry.TryGetValue(tag, out var pageType))
            _frame.Navigate(pageType);
    }

    public bool CanGoBack => _frame?.CanGoBack ?? false;

    public void GoBack() => _frame?.GoBack();
}
