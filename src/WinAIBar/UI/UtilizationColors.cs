namespace WinAIBar.UI;

internal static class UtilizationColors
{
    public static (byte R, byte G, byte B) GetRgb(double utilization) => utilization switch
    {
        < 0.50 => (0x00, 0x78, 0xD4),
        < 0.75 => (0x10, 0x7C, 0x10),
        < 0.90 => (0xFF, 0xB9, 0x00),
        < 1.00 => (0xD1, 0x34, 0x38),
        _      => (0xA4, 0x26, 0x2C),
    };
}
