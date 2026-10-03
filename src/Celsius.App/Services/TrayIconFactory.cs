using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows;

namespace Celsius.App.Services;

/// <summary>
/// Provides the Celsius tray icon from the application logo
/// (<c>Assets/celsius.ico</c>), falling back to a runtime-drawn thermometer if
/// the resource cannot be loaded.
/// </summary>
internal static class TrayIconFactory
{
    private const string IconPackUri = "pack://application:,,,/Assets/celsius.ico";
    private static Icon? _cached;

    /// <summary>Returns the application tray icon.</summary>
    public static Icon CreateIcon()
    {
        if (_cached is not null)
        {
            return (Icon)_cached.Clone();
        }

        var loaded = TryLoadLogoIcon();
        if (loaded is not null)
        {
            _cached = loaded;
            return (Icon)loaded.Clone();
        }

        return CreateFallbackIcon();
    }

    private static Icon? TryLoadLogoIcon()
    {
        try
        {
            var uri = new Uri(IconPackUri, UriKind.Absolute);
            var info = Application.GetResourceStream(uri);
            if (info?.Stream is null)
            {
                return null;
            }

            using var stream = info.Stream;
            // Copy into a MemoryStream the Icon can own for its lifetime.
            var ms = new MemoryStream();
            stream.CopyTo(ms);
            ms.Position = 0;
            return new Icon(ms, new System.Drawing.Size(32, 32));
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Draws a simple thermometer at runtime. Used only when the embedded logo
    /// asset is unavailable so the app always has a tray icon.
    /// </summary>
    private static Icon CreateFallbackIcon(int size = 32)
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
