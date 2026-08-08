namespace Celsius.Models;

/// <summary>
/// Belli bir anda alınan sensör okuma kümesi. Geçmiş kalıcı tutulmaz;
/// her anket döngüsü bu nesneyi sıfırdan doldurur.
/// </summary>
public class SensorSnapshot
{
    public DateTime Timestamp { get; init; } = DateTime.Now;
    public string CpuName { get; init; } = "Bilinmiyor";
    public double? CpuPackageTemp { get; set; }
    public double? CpuMaxCoreTemp { get; set; }
    public List<double> CoreTemps { get; init; } = new();
    public double? CpuLoad { get; set; }
    public double? GpuTemp { get; set; }
    public double? GpuLoad { get; set; }
    public double? MaxFanRpm { get; set; }
    public double? Voltage { get; set; }
    public bool HasSensors { get; init; } = true;
}