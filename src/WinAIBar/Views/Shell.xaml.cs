using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinAIBar.Core.ViewModels;
using WinAIBar.Services.Navigation;
using Windows.UI.ViewManagement;

namespace WinAIBar.Views;

public sealed partial class Shell : UserControl
{
    private readonly UISettings _uiSettings = new();
    public ShellViewModel ViewModel { get; }

    public Shell(ShellViewModel viewModel, INavigationFrame navigationFrame)
    {
        ArgumentNullException.ThrowIfNull(viewModel);
        ArgumentNullException.ThrowIfNull(navigationFrame);
        ViewModel = viewModel;
        InitializeComponent();
        navigationFrame.Initialize(ContentFrame);
        NavView.SelectedItem = NavView.MenuItems[0];
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e) =>
        _uiSettings.ColorValuesChanged += OnColorValuesChanged;

    private void OnUnloaded(object sender, RoutedEventArgs e) =>
        _uiSettings.ColorValuesChanged -= OnColorValuesChanged;

    private void OnColorValuesChanged(UISettings sender, object args) =>
        DispatcherQueue.TryEnqueue(() => RequestedTheme = ElementTheme.Default);

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem { Tag: string tag })
            ViewModel.NavigateCommand.Execute(tag);
    }
}
