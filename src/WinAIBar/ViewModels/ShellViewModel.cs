using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinAIBar.Services.Navigation;

namespace WinAIBar.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;
    private object? _selectedItem;

    public ShellViewModel(INavigationService navigationService)
    {
        _navigationService = navigationService;
    }

    public object? SelectedItem
    {
        get => _selectedItem;
        set => SetProperty(ref _selectedItem, value);
    }

    [RelayCommand]
    private void Navigate(string tag) => _navigationService.NavigateTo(tag);
}
