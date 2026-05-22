using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinAIBar.Core.Services.Navigation;

namespace WinAIBar.Core.ViewModels;

public partial class ShellViewModel(INavigationService navigationService) : ObservableObject
{
    private readonly INavigationService _navigationService =
        navigationService ?? throw new ArgumentNullException(nameof(navigationService));

    private object? _selectedItem;

    public object? SelectedItem
    {
        get => _selectedItem;
        set => SetProperty(ref _selectedItem, value);
    }

    [RelayCommand]
    private void Navigate(string tag) => _navigationService.NavigateTo(tag);
}
