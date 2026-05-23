using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using WinAIBar.Core.Services.Navigation;

namespace WinAIBar.Services.Navigation;

internal sealed partial class NavigationService(IPageRouter router, ILogger<NavigationService> logger)
    : INavigationService, INavigationFrame
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
        if (pageType is null)
        {
            LogUnknownTag(logger, tag);
            return;
        }
        if (_frame is not null && _frame.Content?.GetType() != pageType)
            _frame.Navigate(pageType);
    }

    public bool CanGoBack => _frame?.CanGoBack ?? false;

    public void GoBack() => _frame?.GoBack();

    [LoggerMessage(Level = LogLevel.Warning, Message = "NavigateTo: no page registered for tag '{Tag}'")]
    private static partial void LogUnknownTag(ILogger logger, string tag);
}
