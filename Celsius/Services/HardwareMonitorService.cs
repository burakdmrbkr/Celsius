using Celsius.Models;
using LibreHardwareMonitor.Hardware;

namespace Celsius.Services;

/// <summary>
/// LibreHardwareMonitor üzerinden anlık sensör okumalarını toplar.
/// Akıllı filtreleme: 0/dead okuyan sensörler (örn. bazı AMD anakartlarında SMU sıcaklığı 0 döner)
/// geçersiz sayılır ve gösterilmez. GPU'da Hot Spot/Memory yerine GPU Core tercih edilir.
/// En iyi sonuç için uygulama Yönetici olarak çalışmalıdır.
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
            if (snap.CpuPackageTemp is not null || snap.CpuMaxCoreTemp is not null)
                snap.TempSource = "LibreHardwareMonitor";

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

    /// <summary>
    /// CPU sensörlerini okur. AMD'de paket sıcaklığı "Core (Tctl/Tdie)", Intel'de "CPU Package" + "Core #n" olur.
    /// 0 değer (ölü/erişilemeyen SMU) geçersiz sayılır.
    /// </summary>
    private static void ReadCpu(IHardware cpu, SensorSnapshot snap)
    {
        foreach (var s in cpu.Sensors)
        {
            switch (s.SensorType)
            {
                case SensorType.Temperature:
                    // 0 okuyan sensörleri yoksay (bazı AMD anakartlarda SMU sıcaklık 0 döner)
                    if (!(s.Value is { } v) || v <= 0) break;
                    var name = s.Name;
                    if (name.Contains("Tctl", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("Tdie", StringComparison.OrdinalIgnoreCase) ||
                        name.Contains("Package", StringComparison.OrdinalIgnoreCase))
                    {
                        // AMD: "Core (Tctl/Tdie)" → paket sıcaklığı
                        snap.CpuPackageTemp ??= v;
                    }
                    else if (name.StartsWith("Core", StringComparison.OrdinalIgnoreCase))
                    {
                        // Intel: "Core #1", "Core #2", ...
                        snap.CoreTemps.Add(v);
                    }
                    else if (name.StartsWith("CPU", StringComparison.OrdinalIgnoreCase) && snap.CpuPackageTemp is null)
                    {
                        snap.CpuPackageTemp = v;
                    }
                    break;

                case SensorType.Load:
                    if (s.Name.Contains("Total", StringComparison.OrdinalIgnoreCase))
                        snap.CpuLoad = s.Value;
                    else if (s.Name.StartsWith("CPU Core", StringComparison.OrdinalIgnoreCase) &&
                             !s.Name.Contains("Max", StringComparison.OrdinalIgnoreCase) &&
                             s.Value is { } loadVal)
                        snap.CoreLoads.Add(loadVal);
                    break;

                case SensorType.Voltage:
                    if (s.Name.Contains("Core", StringComparison.OrdinalIgnoreCase) && snap.Voltage is null)
                        snap.Voltage = s.Value;
                    break;
            }
        }
        snap.CpuMaxCoreTemp = snap.CoreTemps.Count > 0 ? snap.CoreTemps.Max() : snap.CpuPackageTemp;
    }

    /// <summary>GPU'dan "GPU Core" sıcaklığını tercih eder (Hot Spot / Memory Junction'ı değil).</summary>
    private static void ReadGpu(IHardware gpu, SensorSnapshot snap)
    {
        var temps = gpu.Sensors
            .Where(s => s.SensorType == SensorType.Temperature && s.Value is { } v && v > 0)
            .ToList();

        var coreTemp = temps.FirstOrDefault(s =>
            s.Name.Contains("Core", StringComparison.OrdinalIgnoreCase) &&
            !s.Name.Contains("Hot", StringComparison.OrdinalIgnoreCase) &&
            !s.Name.Contains("Memory", StringComparison.OrdinalIgnoreCase) &&
            !s.Name.Contains("Junction", StringComparison.OrdinalIgnoreCase) &&
            !s.Name.Contains("VRAM", StringComparison.OrdinalIgnoreCase));

        snap.GpuTemp = coreTemp is not null ? coreTemp.Value
            : (temps.Count > 0 ? temps.Max(s => s.Value) : null);

        var loads = gpu.Sensors.Where(s => s.SensorType == SensorType.Load && s.Value.HasValue).ToList();
        var coreLoad = loads.FirstOrDefault(s => s.Name.Contains("Core", StringComparison.OrdinalIgnoreCase));
        snap.GpuLoad = coreLoad?.Value ?? (loads.Count > 0 ? loads.Max(s => s.Value) : null);
    }

    private double? ReadMaxFanRpm()
    {
        double? max = null;
        foreach (var hw in AllHardware(_computer))
        {
            foreach (var s in hw.Sensors)
            {
                if (s.SensorType == SensorType.Fan && s.Value is { } v && v > 0)
                    max = Math.Max(max ?? 0, v);
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