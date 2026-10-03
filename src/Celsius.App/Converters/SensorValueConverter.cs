using System.Globalization;
using System.Windows.Data;
using Celsius.App.Localization;

namespace Celsius.App.Converters;

/// <summary>Formats a nullable float with a unit, showing an em dash when missing.</summary>
public sealed class SensorValueConverter : IValueConverter
{
    /// <summary>Unit suffix, set from XAML (e.g. "°C", "%", "MHz").</summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>Number of decimal places.</summary>
    public int Decimals { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not float f || float.IsNaN(f))
        {
            return LocalizationManager.Get("ValueUnavailable");
        }

        var text = Decimals > 0
            ? f.ToString("F" + Decimals, CultureInfo.CurrentCulture)
            : MathF.Round(f).ToString(CultureInfo.CurrentCulture);

        return string.IsNullOrEmpty(Unit) ? text : $"{text} {Unit}";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Highlights a temperature value in warning colours above a threshold.</summary>
public sealed class TemperatureBrushConverter : IValueConverter
{
    /// <summary>Temperature at or above which the "hot" brush is used.</summary>
    public double HotThreshold { get; set; } = 85;

    /// <summary>Temperature at or above which the "warm" brush is used.</summary>
    public double WarmThreshold { get; set; } = 70;

    /// <summary>Colour applied below <see cref="WarmThreshold"/>.</summary>
    public string NormalColor { get; set; } = "#22C55E";

    /// <summary>Colour applied between warm and hot thresholds.</summary>
    public string WarmColor { get; set; } = "#F59E0B";

    /// <summary>Colour applied at or above <see cref="HotThreshold"/>.</summary>
    public string HotColor { get; set; } = "#EF4444";

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var color = ResolveColor(value as float? ?? float.NaN, HotThreshold, WarmThreshold, NormalColor, WarmColor, HotColor);
        return new System.Windows.Media.BrushConverter().ConvertFromString(color)
            as System.Windows.Media.Brush
            ?? System.Windows.Media.Brushes.White;
    }

    /// <summary>
    /// Resolves the temperature colour for a value. Shared by the XAML converter
    /// and code-behind so both stay consistent.
    /// </summary>
    public static string ResolveColor(
        float? value,
        double hotThreshold,
        double warmThreshold,
        string normalColor,
        string warmColor,
        string hotColor)
    {
        if (value is not { } f || float.IsNaN(f))
        {
            return normalColor;
        }

        if (f >= hotThreshold)
        {
            return hotColor;
        }

        return f >= warmThreshold ? warmColor : normalColor;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
