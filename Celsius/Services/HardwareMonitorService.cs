using Celsius.Models;
using LibreHardwareMonitor.Hardware;

namespace Celsius.Services;

/// <summary>
/// LibreHardwareMonitor üzerinden anlık sensör okumalarını toplar.
/// LibreHardwareMonitor'un Storage desteği sayesinde disk SMART verisini de sağlar.
/// En iyi sonuç için uygulama Yönetici olarak çalışmalıdır (WinRing0 sürücüsü).
/// </summary>
public sealed class HardwareMonitorService : IDisposable
{
    private readonly Computer _computer = new()
    {
        IsCpuEnabled = true,
        IsGpuEnabled = true,
        IsMotherboardEnabled = true,
        IsStorageEnabled = true
    };

    private readonly DiskSmartService _diskBuilder = new();
    private readonly object _lock = new();
    private SensorSnapshot _latest = new();
    private IReadOnlyList<DiskHealthInfo> _disks = Array.Empty<DiskHealthInfo>();

    public HardwareMonitorService()
    {
        try { _computer.Open(); }
        catch { _latest = new SensorSnapshot { HasSensors = false }; }
    }

    public SensorSnapshot Latest
    {
        get { lock (_lock) return _latest; }
    }

    public IReadOnlyList<DiskHealthInfo> Disks
    {
        get { lock (_lock) return _disks; }
    }

    /// <summary>Donanımı bir kez yenileyip güncel okumaları alır.</summary>
    public void Poll()
    {
        SensorSnapshot snap;
        IReadOnlyList<DiskHealthInfo> disks;
        try
        {
            _computer.Accept(new UpdateVisitor());

            var cpu = Find(HardwareType.Cpu);
            snap = new SensorSnapshot { CpuName = cpu?.Name ?? "Bilinmiyor" };
            if (cpu is not null) ReadCpu(cpu, snap);

            var gpu = Find(HardwareType.GpuNvidia) ?? Find(HardwareType.GpuAmd) ?? Find(HardwareType.GpuIntel);
            if (gpu is not null) ReadGpu(gpu, snap);

            snap.MaxFanRpm = ReadMaxFanRpm();
            disks = _diskBuilder.Read(_computer);
        }
        catch
        {
            snap = new SensorSnapshot { HasSensors = false };
            disks = Array.Empty<DiskHealthInfo>();
        }

        lock (_lock)
        {
            _latest = snap;
            _disks = disks;
        }
    }

    private IHardware? Find(HardwareType type)
    {
        foreach (var hw in AllHardware(_computer))
            if (hw.HardwareType == type) return hw;
        return null;
    }

    private static IEnumerable<IHardware> AllHardware(IComputer computer)
    {
        foreach (var hw in computer.Hardware)
        {
            yield return hw;
            foreach (var sub in hw.SubHardware) yield return sub;
        }
    }

    private static void ReadCpu(IHardware cpu, SensorSnapshot snap)
    {
        foreach (var s in cpu.Sensors)
        {
            switch (s.SensorType)
            {
                case SensorType.Temperature:
                    if (!s.Value.HasValue) break;
                    if (s.Name.Contains("Package", StringComparison.OrdinalIgnoreCase) ||
                        s.Name.Contains("Tctl", StringComparison.OrdinalIgnoreCase) ||
                        s.Name.Contains("Tdie", StringComparison.OrdinalIgnoreCase))
                    {
                        snap.CpuPackageTemp ??= s.Value;
                    }
                    if (s.Name.StartsWith("Core", StringComparison.OrdinalIgnoreCase))
                        snap.CoreTemps.Add(s.Value.Value);
                    if (s.Name.StartsWith("CPU", StringComparison.OrdinalIgnoreCase) && snap.CpuPackageTemp is null)
                        snap.CpuPackageTemp = s.Value; // AMD: "CPU Core (Tctl/Tdie)"
                    break;

                case SensorType.Load:
                    if (s.Name.Contains("Total", StringComparison.OrdinalIgnoreCase))
                        snap.CpuLoad = s.Value;
                    break;

                case SensorType.Voltage:
                    if (s.Name.Contains("Core", StringComparison.OrdinalIgnoreCase) && snap.Voltage is null)
                        snap.Voltage = s.Value;
                    break;
            }
        }
        snap.CpuMaxCoreTemp = snap.CoreTemps.Count > 0 ? snap.CoreTemps.Max() : snap.CpuPackageTemp;
    }

    private static void ReadGpu(IHardware gpu, SensorSnapshot snap)
    {
        var temps = gpu.Sensors.Where(s => s.SensorType == SensorType.Temperature && s.Value.HasValue).ToList();
        var loads = gpu.Sensors.Where(s => s.SensorType == SensorType.Load && s.Value.HasValue).ToList();
        snap.GpuTemp = temps.Count > 0 ? temps.Max(s => s.Value) : null;
        snap.GpuLoad = loads.Count > 0 ? loads.Max(s => s.Value) : null;
    }

    private double? ReadMaxFanRpm()
    {
        double? max = null;
        foreach (var hw in AllHardware(_computer))
        {
            foreach (var s in hw.Sensors)
            {
                if (s.SensorType == SensorType.Fan && s.Value.HasValue)
                    max = Math.Max(max ?? 0, s.Value.Value);
            }
        }
        return max;
    }

    /// <summary>Bilinen bir IVisitor uygulaması: tüm donanım öğelerini günceller.</summary>
    private sealed class UpdateVisitor : IVisitor
    {
        public void VisitComputer(IComputer computer) => computer.Traverse(this);

        public void VisitHardware(IHardware hardware)
        {
            hardware.Update();
            foreach (var sub in hardware.SubHardware) sub.Accept(this);
        }

        public void VisitSensor(ISensor sensor) { }
        public void VisitParameter(IParameter parameter) { }
    }

    public void Dispose()
    {
        try { _computer.Close(); }
        catch { /* zaten kapalı olabilir */ }
    }
}
