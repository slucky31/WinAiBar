using WinAIBar.Views.Pages;

namespace WinAIBar.Services.Navigation;

internal sealed class PageRouter : IPageRouter
{
    private static readonly Dictionary<string, Type> Registry = new()
    {
        ["Dashboard"] = typeof(DashboardPage),
        ["Claude"] = typeof(ClaudePage),
        ["Copilot"] = typeof(CopilotPage),
        ["History"] = typeof(HistoryPage),
        ["Health"] = typeof(HealthPage),
        ["Cost"] = typeof(CostPage),
        ["Settings"] = typeof(SettingsPage),
    };

    public Type? Resolve(string tag) =>
        Registry.TryGetValue(tag, out var pageType) ? pageType : null;
}
