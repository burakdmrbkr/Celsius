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
    /// <summary>Per-CCD sıcaklıkları (AMD) — per-core sıcaklık sensörü olmayan CPU'larda kullanılır.</summary>
    public List<CcdTempInfo> CcdTemps { get; init; } = new();
    public double? CpuLoad { get; set; }
    public double? GpuTemp { get; set; }
    public double? GpuLoad { get; set; }
    public double? MaxFanRpm { get; set; }
    public double? Voltage { get; set; }
    public bool HasSensors { get; init; } = true;

    /// <summary>Sıcaklık verisinin geldiği kaynak: "HWiNFO", "LibreHardwareMonitor" ya da boş.</summary>
    public string TempSource { get; set; } = "";
}

/// <summary>AMD'de per-CCD sıcaklık okuması (ör. "CCD1 (Tdie)" → Label "CCD1").</summary>
public sealed record CcdTempInfo(string Label, double Temp);