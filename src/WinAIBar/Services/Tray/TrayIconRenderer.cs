using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace WinAIBar.Services.Tray;

[SupportedOSPlatform("windows5.1.2600")]
public sealed class TrayIconRenderer : ITrayIconRenderer
{
    private const int Size = 32;

    public Icon Render(double maxUtilization)
    {
        var fill = GetFillColor(maxUtilization);
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

    private static Color GetFillColor(double u) => u switch
    {
        < 0.50 => Color.FromArgb(0x00, 0x78, 0xD4),
        < 0.75 => Color.FromArgb(0x10, 0x7C, 0x10),
        < 0.90 => Color.FromArgb(0xFF, 0xB9, 0x00),
        < 1.00 => Color.FromArgb(0xD1, 0x34, 0x38),
        _      => Color.FromArgb(0xA4, 0x26, 0x2C),
    };

    [DllImport("user32.dll", ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr hIcon);
}
