namespace Celsius.Models;

/// <summary>
/// SMART verilerinden türetilen disk sağlık özeti (CrystalDiskInfo tarzı).
/// </summary>
public class DiskHealthInfo
{
    public string Name { get; set; } = "";
    public string MediaType { get; set; } = "Bilinmiyor";
    public double? CapacityGb { get; set; }
    public double? Temperature { get; set; }
    public int HealthPercent { get; set; } = 100;
    public string Status { get; set; } = "İyi";
    public double? PowerOnHours { get; set; }
    public double? ReallocatedSectors { get; set; }
    public double? PendingSectors { get; set; }

    /// <summary>Sağlık yüzdesine göre renk (karttaki ilerleme çubuğu için).</summary>
    public System.Windows.Media.Brush HealthBrush => HealthPercent switch
    {
        >= 90 => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(63, 185, 80)),
        >= 70 => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(210, 153, 34)),
        >= 50 => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 145, 0)),
        _ => new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(248, 81, 73))
    };

    /// <summary>Sıcaklık metni (yoksa "—")</summary>
    public string TempText => Temperature.HasValue ? $"{Temperature.Value:0.0} °C" : "—";
    public string CapacityText => CapacityGb.HasValue ? $"{CapacityGb.Value:0} GB" : "—";
    public string PohText => PowerOnHours.HasValue ? $"{PowerOnHours.Value:0} saat" : "—";
}