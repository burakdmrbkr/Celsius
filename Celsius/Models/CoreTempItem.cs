using System.Windows.Media;

namespace Celsius.Models;

/// <summary>Her çekirdeğin anlık sıcaklığını UI'da gösterim için saran model.</summary>
public class CoreTempItem
{
    public required string Label { get; init; }
    public double TjMax { get; init; }
    public double Temp { get; set; }

    public string TempText => $"{Temp:0.0} °C";
    public double Ratio => TjMax <= 0 ? 0 : Math.Clamp(Temp / TjMax, 0, 1);

    public Brush Color => Ratio switch
    {
        >= 0.95 => new SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 81, 73)),
        >= 0.80 => new SolidColorBrush(System.Windows.Media.Color.FromRgb(210, 153, 34)),
        >= 0.55 => new SolidColorBrush(System.Windows.Media.Color.FromRgb(63, 185, 80)),
        _ => new SolidColorBrush(System.Windows.Media.Color.FromRgb(139, 148, 158))
    };
}