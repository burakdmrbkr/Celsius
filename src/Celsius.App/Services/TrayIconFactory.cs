using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows;

namespace Celsius.App.Services;

/// <summary>Generates the tray icon at runtime so no binary asset is required.</summary>
internal static class TrayIconFactory
{
    /// <summary>Creates the application tray icon (a stylized thermometer droplet).</summary>
    public static System.Drawing.Icon CreateIcon(int size = 32)
    {
        using var bitmap = new Bitmap(size, size);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(System.Drawing.Color.Transparent);

            var accent = System.Drawing.Color.FromArgb(0x38, 0xBD, 0xF8);
            var bulb = System.Drawing.Color.FromArgb(0xEF, 0x44, 0x44);

            // Thermometer stem.
            var stemRect = new Rectangle(size * 7 / 16, size * 3 / 16, size / 8, size * 9 / 16);
            using (var stemBrush = new SolidBrush(accent))
            using (var path = RoundedRect(stemRect, size / 16))
            {
                g.FillPath(stemBrush, path);
            }

            // Bulb.
            var bulbRect = new Rectangle(size * 3 / 16, size * 10 / 16, size * 10 / 16, size * 10 / 16);
            using var bulbBrush = new SolidBrush(bulb);
            g.FillEllipse(bulbBrush, bulbRect);
        }

        var handle = bitmap.GetHicon();
        try
        {
            using var icon = System.Drawing.Icon.FromHandle(handle);
            return (System.Drawing.Icon)icon.Clone();
        }
        finally
        {
            NativeMethods.DestroyIcon(handle);
        }
    }

    private static GraphicsPath RoundedRect(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        var d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static class NativeMethods
    {
        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        public static extern bool DestroyIcon(IntPtr handle);
    }
}
