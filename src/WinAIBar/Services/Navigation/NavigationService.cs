using Microsoft.UI.Xaml.Controls;
using WinAIBar.Core.Services.Navigation;

namespace WinAIBar.Services.Navigation;

internal sealed class NavigationService(IPageRouter router) : INavigationService, INavigationFrame
{
    private Frame? _frame;

    void INavigationFrame.Initialize(Frame contentFrame)
    {
        ArgumentNullException.ThrowIfNull(contentFrame);
        _frame = contentFrame;
    }

    public void NavigateTo(string tag)
    {
        var pageType = router.Resolve(tag);
        if (_frame is not null && pageType is not null)
            _frame.Navigate(pageType);
    }

    public bool CanGoBack => _frame?.CanGoBack ?? false;

    public void GoBack() => _frame?.GoBack();
}
