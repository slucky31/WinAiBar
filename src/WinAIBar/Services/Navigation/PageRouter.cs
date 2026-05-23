using WinAIBar.Views.Pages;

namespace WinAIBar.Services.Navigation;

internal sealed class PageRouter : IPageRouter
{
    private static readonly Dictionary<string, Type> Registry = new()
    {
        [NavigationTags.Dashboard] = typeof(DashboardPage),
        [NavigationTags.Claude]    = typeof(ClaudePage),
        [NavigationTags.Copilot]   = typeof(CopilotPage),
        [NavigationTags.History]   = typeof(HistoryPage),
        [NavigationTags.Health]    = typeof(HealthPage),
        [NavigationTags.Cost]      = typeof(CostPage),
        [NavigationTags.Settings]  = typeof(SettingsPage),
    };

    public Type? Resolve(string tag) =>
        Registry.TryGetValue(tag, out var pageType) ? pageType : null;
}
