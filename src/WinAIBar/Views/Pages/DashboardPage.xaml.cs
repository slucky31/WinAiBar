using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using WinAIBar.Infrastructure;
using WinAIBar.ViewModels;

namespace WinAIBar.Views.Pages;

public sealed partial class DashboardPage : Page
{
    public DashboardViewModel ViewModel { get; }

    public DashboardPage()
    {
        ViewModel = AppHost.Current.Services.GetRequiredService<DashboardViewModel>();
        InitializeComponent();
    }
}
