using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WinAIBar.ViewModels;

public partial class ShellViewModel : ObservableObject
{
    private Action<string>? _navigateCallback;
    private object? _selectedItem;

    public object? SelectedItem
    {
        get => _selectedItem;
        set => SetProperty(ref _selectedItem, value);
    }

    public void Initialize(Action<string> navigateCallback)
    {
        _navigateCallback = navigateCallback;
    }

    [RelayCommand]
    private void Navigate(string tag)
    {
        _navigateCallback?.Invoke(tag);
    }
}
