using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Windows.Graphics;
using WinAIBar.ViewModels;
using WinRT.Interop;

namespace WinAIBar.Views.Flyouts;

[SupportedOSPlatform("windows10.0.17763.0")]
public sealed partial class TrayFlyout : Window
{
    public TrayFlyoutViewModel ViewModel { get; }

    private bool _hasBeenActivated;

    public TrayFlyout(TrayFlyoutViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();

        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.IsResizable = false;
            presenter.SetBorderAndTitleBar(hasBorder: true, hasTitleBar: false);
        }

        Activated += OnActivated;
    }

    public void ShowAtTaskbar()
    {
        _hasBeenActivated = false;
        PositionAtTaskbar();
        Activate();
    }

    private void PositionAtTaskbar()
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        uint dpi = GetDpiForWindow(hwnd);
        if (dpi == 0) dpi = 96;
        double scale = dpi / 96.0;

        int widthPx = (int)(360 * scale);
        int heightPx = (int)(420 * scale);

        AppWindow.Resize(new SizeInt32(widthPx, heightPx));

        var display = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
        var workArea = display.WorkArea;
        var outer = display.OuterBounds;

        // Detect which edge the taskbar is on by comparing work area to outer bounds.
        // The notification area is at the right end of a horizontal taskbar (top/bottom)
        // and at the bottom of a vertical taskbar (left/right).
        int left, top;
        if (workArea.Y > outer.Y)
        {
            // Taskbar on top: anchor flyout to top-right of work area
            left = workArea.X + workArea.Width - widthPx;
            top = workArea.Y;
        }
        else
        {
            // Taskbar on bottom, left, or right: anchor to bottom-right of work area
            left = workArea.X + workArea.Width - widthPx;
            top = workArea.Y + workArea.Height - heightPx;
        }

        AppWindow.Move(new PointInt32(left, top));
    }

    private void OnActivated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState != WindowActivationState.Deactivated)
        {
            _hasBeenActivated = true;
        }
        else if (_hasBeenActivated)
        {
            _hasBeenActivated = false;
            AppWindow.Hide();
        }
    }

    [DllImport("user32.dll", SetLastError = false)]
    private static extern uint GetDpiForWindow(IntPtr hwnd);
}
