using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using WinAIBar.UI;

namespace WinAIBar.Services.Tray;

[SupportedOSPlatform("windows10.0.17763.0")]
public sealed class TrayIconRenderer : ITrayIconRenderer
{
    private const int Size = 32;

    public Icon Render(double maxUtilization)
    {
        var (cr, cg, cb) = UtilizationColors.GetRgb(maxUtilization);
        var fill = Color.FromArgb(cr, cg, cb);
        var pct = (int)Math.Round(Math.Clamp(maxUtilization * 100.0, 0.0, 999.0));
        var text = $"{pct}%";

        using var bmp = new Bitmap(Size, Size, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        g.Clear(Color.Transparent);

        using (var brush = new SolidBrush(fill))
            g.FillEllipse(brush, 1, 1, Size - 2, Size - 2);

        var fontSize = text.Length >= 4 ? 7.5f : 9f;
        using var font = new Font("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Point);
        var sz = g.MeasureString(text, font);
        float tx = (Size - sz.Width) / 2f;
        float ty = (Size - sz.Height) / 2f;

        using (var white = new SolidBrush(Color.White))
            g.DrawString(text, font, white, tx, ty);

        var hIcon = bmp.GetHicon();
        try
        {
            return (Icon)Icon.FromHandle(hIcon).Clone();
        }
        finally
        {
            DestroyIcon(hIcon);
        }
    }

    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr hIcon);
}
