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
        PositionAtTaskbar();
        Activate();
    }

    private void PositionAtTaskbar()
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        uint dpi = GetDpiForWindow(hwnd);
        double scale = dpi / 96.0;

        int widthPx = (int)(360 * scale);
        int heightPx = (int)(420 * scale);

        AppWindow.Resize(new SizeInt32(widthPx, heightPx));

        var display = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
        var workArea = display.WorkArea;
        int left = workArea.X + workArea.Width - widthPx;
        int top = workArea.Y + workArea.Height - heightPx;
        AppWindow.Move(new PointInt32(left, top));
    }

    private void OnActivated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState == WindowActivationState.Deactivated)
            AppWindow.Hide();
    }

    [DllImport("user32.dll", SetLastError = false)]
    private static extern uint GetDpiForWindow(IntPtr hwnd);
}
