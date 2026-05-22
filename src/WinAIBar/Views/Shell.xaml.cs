using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.UI.ViewManagement;
using WinAIBar.ViewModels;
using WinAIBar.Views.Pages;

namespace WinAIBar.Views;

public sealed partial class Shell : UserControl
{
    private readonly UISettings _uiSettings = new();

    public ShellViewModel ViewModel { get; } = new();

    public Shell()
    {
        InitializeComponent();
        ViewModel.Initialize(NavigateTo);
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        ContentFrame.Navigate(typeof(DashboardPage));
        NavView.SelectedItem = NavView.MenuItems[0];
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _uiSettings.ColorValuesChanged += OnColorValuesChanged;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _uiSettings.ColorValuesChanged -= OnColorValuesChanged;
    }

    private void OnColorValuesChanged(UISettings sender, object args)
    {
        DispatcherQueue.TryEnqueue(() => this.RequestedTheme = ElementTheme.Default);
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem { Tag: string tag })
        {
            NavigateTo(tag);
        }
    }

    private void NavigateTo(string tag)
    {
        var pageType = tag switch
        {
            "Dashboard" => typeof(DashboardPage),
            "Claude" => typeof(ClaudePage),
            "Copilot" => typeof(CopilotPage),
            "History" => typeof(HistoryPage),
            "Health" => typeof(HealthPage),
            "Cost" => typeof(CostPage),
            "Settings" => typeof(SettingsPage),
            _ => typeof(DashboardPage)
        };
        ContentFrame.Navigate(pageType);
    }
}
