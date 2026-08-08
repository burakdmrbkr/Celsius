using Celsius.Models;
using LibreHardwareMonitor.Hardware;

namespace Celsius.Services;

/// <summary>
/// Disk sağlığı özetini SMART sensörlerinden üretir (CrystalDiskInfo tarzı).
/// Gerçek LibreHardwareMonitor Storage sensör düzeni kullanılır:
///   Level "Life" → sağlık yüzdesi · Temperature "Composite" → sıcaklık
///   Factor "Power On Hours" → çalışma saati · Data "Total Space" → kapasite (GB)
/// </summary>
public sealed class DiskSmartService
{
    public IReadOnlyList<DiskHealthInfo> Read(IComputer computer)
    {
        var result = new List<DiskHealthInfo>();
        foreach (var hw in AllHardware(computer))
        {
            if (hw.HardwareType != HardwareType.Storage) continue;
            result.Add(Build(hw));
        }
        return result;
    }

    private static IEnumerable<IHardware> AllHardware(IComputer computer)
    {
        foreach (var hw in computer.Hardware)
        {
            yield return hw;
            foreach (var sub in hw.SubHardware) yield return sub;
        }
    }

    private static DiskHealthInfo Build(IHardware storage)
    {
        var info = new DiskHealthInfo { Name = storage.Name };

        // Sıcaklık: Composite tercih edilir, yoksa ilk geçerli
        var temp = storage.Sensors.FirstOrDefault(s =>
                       s.SensorType == SensorType.Temperature &&
                       s.Name.Contains("Composite", StringComparison.OrdinalIgnoreCase))
                   ?? storage.Sensors.FirstOrDefault(s =>
                       s.SensorType == SensorType.Temperature &&
                       !s.Name.Contains("Warning", StringComparison.OrdinalIgnoreCase) &&
                       !s.Name.Contains("Critical", StringComparison.OrdinalIgnoreCase) &&
                       s.Value.HasValue);
        info.Temperature = temp?.Value;

        foreach (var s in storage.Sensors)
        {
            if (!s.Value.HasValue) continue;
            var value = s.Value.Value;

            switch (s.SensorType)
            {
                case SensorType.Level:
                    if (s.Name.Equals("Life", StringComparison.OrdinalIgnoreCase))
                        info.HealthPercent = ClampHealth(value);
                    else if (s.Name.Contains("Available Spare", StringComparison.OrdinalIgnoreCase) &&
                             !s.Name.Contains("Threshold", StringComparison.OrdinalIgnoreCase))
                        info.HealthPercent = Math.Min(info.HealthPercent, ClampHealth(value));
                    break;

                case SensorType.Factor:
                    if (s.Name.Contains("Power On Hours", StringComparison.OrdinalIgnoreCase))
                        info.PowerOnHours = value;
                    break;

                case SensorType.Data:
                    var n = s.Name.ToLowerInvariant();
                    if (n.Contains("total space"))
                        info.CapacityGb = value;
                    else if (n.Contains("reallocated") || n.Contains("uncorrectable") || n.Contains("pending"))
                        info.PendingSectors = value;
                    break;
            }
        }

        info.Status = info.HealthPercent switch
        {
            >= 90 => "İyi",
            >= 70 => "Dikkat",
            >= 50 => "Riskli",
            _ => "Kritik"
        };

        return info;
    }

    private static int ClampHealth(double v) => (int)Math.Clamp(Math.Round(v), 0, 100);
}
