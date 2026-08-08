using Celsius.Models;
using LibreHardwareMonitor.Hardware;

namespace Celsius.Services;

/// <summary>
/// CrystallDiskInfo tarzı disk sağlığı özetini SMART sensörlerinden üretir.
/// Ham SMART öznitelikleri LibreHardwareMonitor'un Storage sensörleri olarak kullanılabilir.
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

        var tempSensor = storage.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature);
        info.Temperature = tempSensor?.Value;

        foreach (var s in storage.Sensors)
        {
            if (s.SensorType != SensorType.Data || !s.Value.HasValue) continue;
            var name = s.Name.ToLowerInvariant();
            var value = s.Value.Value;

            if (name.Contains("remaining life") || name.Contains("health status"))
                info.HealthPercent = (int)Math.Clamp(Math.Round(value), 0, 100);
            else if (name.Contains("reallocated"))           // Reallocated Sectors Count
                info.ReallocatedSectors = value;
            else if (name.Contains("uncorrectable") || name.Contains("pending")) // Pending/Uncorrectable
                info.PendingSectors = value;
            else if (name.Contains("power on") || name.Contains("start stop count"))
                info.PowerOnHours ??= value;
            else if (name.Contains("media wear") || name.Contains("used reserved") || name.Contains("erase fail"))
                info.PowerOnHours ??= value; // SSD ömrü göstergesi (opsiyonel)
        }

        // SSD/HDD ayrımı: "media" olarak tanımlıysa MediaType kullan
        foreach (var d in storage.SubHardware) { /* Storage alt-donanımı yoktur; tutarlılık için. */ }

        info.Status = info.HealthPercent switch
        {
            >= 90 => "İyi",
            >= 70 => "Dikkat",
            >= 50 => "Riskli",
            _ => "Kritik"
        };

        return info;
    }
}