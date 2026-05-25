using System.Drawing;

namespace WinAIBar.Services.Tray;

public interface ITrayIconRenderer
{
    Icon Render(double maxUtilization);
}
