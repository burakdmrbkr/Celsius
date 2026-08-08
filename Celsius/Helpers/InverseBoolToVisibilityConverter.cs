using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Celsius.Helpers;

/// <summary>bool → Visibility; true'da Collapsed (ters çevirir). "Disk verisi yok" uyarısı için.</summary>
public class InverseBoolToVisibilityConverter : IValueConverter
{
    public static readonly InverseBoolToVisibilityConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b && b ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility v && v == Visibility.Collapsed;
}
