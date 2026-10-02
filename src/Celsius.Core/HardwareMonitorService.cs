using Celsius.Core.Gpu;
using Celsius.Core.Models;
using Celsius.Core.Storage;
using LibreHardwareMonitor.Hardware;

namespace Celsius.Core;

/// <summary>
/// <see cref="IHardwareMonitor"/> implementation backed by LibreHardwareMonitor.
/// A single <see cref="Computer"/> instance is polled on a background thread;
/// this class itself is thread-safe.
/// </summary>
public sealed class HardwareMonitorService : IHardwareMonitor
{
    private readonly object _sync = new();
    private readonly DiskInfoProvider _diskProvider;
    private readonly GpuInfoProvider _gpuProvider;

    private Computer? _computer;
    private UpdateVisitor? _visitor;
    private bool _initialized;
    private bool _disposed;

    // Cached GPU identity, correlated with LHM hardware by PCI device id.
    private IReadOnlyList<GpuInfo> _gpuInfos = [];
    private DateTime _gpuInfosRefreshedUtc = DateTime.MinValue;

    // Best-effort memory reading, refreshed periodically.
    private float? _lastMemoryPercent;

    /// <summary>Creates the service with default providers.</summary>
    public HardwareMonitorService()
        : this(new DiskInfoProvider(), new GpuInfoProvider())
    {
    }

    /// <summary>Creates the service with explicit providers (testable).</summary>
    public HardwareMonitorService(DiskInfoProvider diskProvider, GpuInfoProvider gpuProvider)
    {
        _diskProvider = diskProvider ?? throw new ArgumentNullException(nameof(diskProvider));
        _gpuProvider = gpuProvider ?? throw new ArgumentNullException(nameof(gpuProvider));
    }

    /// <inheritdoc />
    public bool HasTemperatureSensors { get; private set; }

    /// <summary>
    /// Message of the last failure raised by the backend (e.g. the kernel driver
    /// could not be loaded). <c>null</c> when the last operation succeeded.
    /// </summary>
    public string? LastError { get; private set; }

    /// <inheritdoc />
    public void Initialize()
    {
        lock (_sync)
        {
            if (_initialized || _disposed)
            {
                return;
            }

            _visitor = new UpdateVisitor();
            _computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
                IsMemoryEnabled = true,
                // Motherboard/SuperIO sensors are needed for board, DIMM and some
                // laptop GPU thermals; storage is needed for NVMe temperature.
                IsStorageEnabled = true,
                IsMotherboardEnabled = true,
                IsControllerEnabled = false,
                IsNetworkEnabled = false,
                IsBatteryEnabled = false,
                IsPsuEnabled = false,
            };

            try
            {
                _computer.Open();
                _computer.Accept(_visitor);

                // "HasTemperatureSensors" must reflect that at least one CPU
                // *temperature* sensor actually produced a value, not merely that
                // a CPU hardware node exists (it always does, even when the
                // kernel driver failed to load).
                HasTemperatureSensors = HasAnyCpuTemperature();
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                HasTemperatureSensors = false;
            }

            _initialized = true;
        }
    }

    /// <inheritdoc />
    public void Refresh()
    {
        lock (_sync)
        {
            if (!_initialized || _disposed || _computer is null || _visitor is null)
            {
                return;
            }

            // Clear before each pass so a recovered device stops reporting an
            // error, while a failing one re-sets it below.
            LastError = null;

            try
            {
                foreach (var hardware in _computer.Hardware)
                {
                    try
                    {
                        hardware.Update();
                    }
                    catch (Exception ex)
                    {
                        // One misbehaving device (e.g. a sensor backend that
                        // throws) must not stop the others from updating.
                        LastError = $"{hardware.Name}: {ex.Message}";
                        continue;
                    }

                    foreach (var sub in hardware.SubHardware)
                    {
                        try
                        {
                            sub.Update();
                        }
                        catch (Exception ex)
                        {
                            LastError = $"{sub.Name}: {ex.Message}";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // A failure enumerating the hardware tree must not tear down the
                // monitor, but we surface it so the UI/log can explain missing
                // values.
                LastError = ex.Message;
            }
        }
    }

    /// <inheritdoc />
    public SystemSnapshot Capture()
    {
        lock (_sync)
        {
            if (!_initialized || _disposed || _computer is null)
            {
                return SystemSnapshot.Empty;
            }

            EnsureGpuInfos();

            float? cpuTemp = null;
            float? cpuClock = null;
            float? cpuLoad = null;
            float? memoryPercent = null;

            foreach (var hardware in _computer.Hardware)
            {
                switch (hardware.HardwareType)
                {
                    case HardwareType.Cpu:
                        ReadCpu(hardware, ref cpuTemp, ref cpuClock, ref cpuLoad);
                        break;

                    case HardwareType.Memory:
                        memoryPercent ??= ReadMemoryPercent(hardware);
                        break;
                }
            }

            if (memoryPercent is not null)
            {
                _lastMemoryPercent = memoryPercent;
            }

            var gpus = BuildGpuReadings();
            var drives = _diskProvider.GetDrives();

            return new SystemSnapshot
            {
                CpuTemperatureC = cpuTemp,
                CpuClockMhz = cpuClock,
                CpuLoadPercent = cpuLoad,
                MemoryUsedPercent = memoryPercent ?? _lastMemoryPercent,
                Drives = drives,
                Gpus = gpus,
            };
        }
    }

    /// <inheritdoc />
    public float? TryGetTjMaxC()
    {
        lock (_sync)
        {
            if (!_initialized || _disposed || _computer is null)
            {
                return null;
            }

            foreach (var hardware in _computer.Hardware)
            {
                if (hardware.HardwareType != HardwareType.Cpu)
                {
                    continue;
                }

                foreach (var sensor in hardware.Sensors)
                {
                    if (sensor.SensorType != SensorType.Temperature)
                    {
                        continue;
                    }

                    // LHM exposes the hardware TjMax as the first sensor parameter
                    // of the temperature sensors (name "TjMax [°C]").
                    if (sensor.Parameters.Count > 0
                        && sensor.Parameters[0].Name.Contains("TjMax", StringComparison.OrdinalIgnoreCase))
                    {
                        var value = sensor.Parameters[0].Value;
                        if (value is > 60f and <= 125f)
                        {
                            return value;
                        }
                    }
                }
            }

            return null;
        }
    }

    private void ReadCpu(IHardware cpu, ref float? temp, ref float? clock, ref float? load)
    {
        float? packageTemp = null;
        float? controlTemp = null;
        float? maxCoreTemp = null;
        float? anyTemp = null;
        float? maxClock = null;
        float? maxLoad = null;

        foreach (var sensor in cpu.Sensors)
        {
            if (sensor.Value is not { } value)
            {
                continue;
            }

            switch (sensor.SensorType)
            {
                case SensorType.Temperature:
                    // Track every plausible thermal reading so we can fall back to
                    // *any* temperature when the usual names are absent (e.g. some
                    // AMD/Intel SKUs expose only "Tccd" or vendor-specific names).
                    if (IsPlausibleTemperature(value))
                    {
                        anyTemp = SelectHigher(anyTemp, value);
                    }

                    var name = sensor.Name;
                    if (name.Contains("Package", StringComparison.OrdinalIgnoreCase)
                        || name.Contains("Tctl", StringComparison.OrdinalIgnoreCase)
                        || name.Contains("CPU", StringComparison.OrdinalIgnoreCase))
                    {
                        packageTemp = SelectHigher(packageTemp, value);
                        if (name.Contains("Tctl", StringComparison.OrdinalIgnoreCase))
                        {
                            controlTemp = SelectHigher(controlTemp, value);
                        }
                    }
                    else if (name.Contains("Core", StringComparison.OrdinalIgnoreCase)
                        || name.Contains("Tdie", StringComparison.OrdinalIgnoreCase)
                        || name.Contains("Tccd", StringComparison.OrdinalIgnoreCase))
                    {
                        maxCoreTemp = SelectHigher(maxCoreTemp, value);
                    }

                    break;

                case SensorType.Clock:
                    if (sensor.Name.Contains("Core", StringComparison.OrdinalIgnoreCase)
                        || sensor.Name.Contains("Bus", StringComparison.OrdinalIgnoreCase) is false)
                    {
                        maxClock = SelectHigher(maxClock, value);
                    }

                    break;

                case SensorType.Load:
                    if (sensor.Name.Contains("Total", StringComparison.OrdinalIgnoreCase))
                    {
                        maxLoad = SelectHigher(maxLoad, value);
                    }

                    break;
            }
        }

        temp = packageTemp ?? controlTemp ?? maxCoreTemp ?? anyTemp;
        clock = maxClock;
        load = maxLoad;

        if (temp is not null)
        {
            HasTemperatureSensors = true;
        }
    }

    /// <summary>Filters out obviously bogus temperature values.</summary>
    private static bool IsPlausibleTemperature(float value) => value is > 0f and < 150f;

    /// <summary>
    /// Returns true when at least one CPU temperature sensor currently yields a
    /// plausible value. Unlike a mere presence check, this confirms the kernel
    /// driver is actually working.
    /// </summary>
    private bool HasAnyCpuTemperature()
    {
        if (_computer is null)
        {
            return false;
        }

        foreach (var hardware in _computer.Hardware)
        {
            if (hardware.HardwareType != HardwareType.Cpu)
            {
                continue;
            }

            foreach (var sensor in hardware.Sensors)
            {
                if (sensor.SensorType == SensorType.Temperature
                    && sensor.Value is { } value
                    && IsPlausibleTemperature(value))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static float? ReadMemoryPercent(IHardware memory)
    {
        float? used = null;
        foreach (var sensor in memory.Sensors)
        {
            if (sensor.SensorType == SensorType.Load
                && sensor.Name.Contains("Memory", StringComparison.OrdinalIgnoreCase)
                && sensor.Value is { } value)
            {
                used = SelectHigher(used, value);
            }
        }

        return used;
    }

    private IReadOnlyList<GpuReading> BuildGpuReadings()
    {
        var readings = new List<GpuReading>();

        foreach (var hardware in _computer!.Hardware)
        {
            if (!IsGpuHardware(hardware.HardwareType))
            {
                continue;
            }

            float? temp = null;
            float? fallbackTemp = null;
            float? clock = null;

            foreach (var sensor in hardware.Sensors)
            {
                if (sensor.Value is not { } value)
                {
                    continue;
                }

                switch (sensor.SensorType)
                {
                    case SensorType.Temperature:
                        if (!IsPlausibleTemperature(value))
                        {
                            break;
                        }

                        // Prefer the core/edge sensor; keep any other plausible
                        // reading as a fallback (e.g. "Hot Spot" only GPUs,
                        // integrated adapters reporting just "GPU").
                        if (sensor.Name.Contains("Core", StringComparison.OrdinalIgnoreCase)
                            || sensor.Name.Contains("Edge", StringComparison.OrdinalIgnoreCase)
                            || sensor.Name.Contains("GPU", StringComparison.OrdinalIgnoreCase))
                        {
                            if (sensor.Name.Contains("Hot", StringComparison.OrdinalIgnoreCase))
                            {
                                fallbackTemp = SelectHigher(fallbackTemp, value);
                            }
                            else
                            {
                                temp = SelectHigher(temp, value);
                            }
                        }
                        else
                        {
                            fallbackTemp = SelectHigher(fallbackTemp, value);
                        }

                        break;

                    case SensorType.Clock
                        when sensor.Name.Contains("Core", StringComparison.OrdinalIgnoreCase)
                             || sensor.Name.Contains("GPU", StringComparison.OrdinalIgnoreCase):
                        clock = SelectHigher(clock, value);
                        break;
                }
            }

            temp ??= fallbackTemp;

            var vendor = MapHardwareVendor(hardware.HardwareType);
            var info = _gpuInfos.FirstOrDefault(g => g.Vendor == vendor);
            var name = info?.Name ?? hardware.Name;

            readings.Add(new GpuReading(
                Name: name,
                Vendor: vendor,
                TemperatureC: temp,
                ClockMhz: clock,
                IsDiscrete: info?.IsDiscrete ?? vendor == GpuVendor.Nvidia));
        }

        return readings;
    }

    private void EnsureGpuInfos()
    {
        var now = DateTime.UtcNow;
        if (_gpuInfos.Count > 0 && (now - _gpuInfosRefreshedUtc) < TimeSpan.FromMinutes(5))
        {
            return;
        }

        _gpuInfos = _gpuProvider.EnumerateGpus();
        _gpuInfosRefreshedUtc = now;
    }

    private static bool IsCpuHardware(IHardware hardware) =>
        hardware.HardwareType == HardwareType.Cpu;

    private static bool IsGpuHardware(HardwareType type) =>
        type is HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel;

    private static GpuVendor MapHardwareVendor(HardwareType type) => type switch
    {
        HardwareType.GpuNvidia => GpuVendor.Nvidia,
        HardwareType.GpuAmd => GpuVendor.Amd,
        HardwareType.GpuIntel => GpuVendor.Intel,
        _ => GpuVendor.Unknown,
    };

    private static float SelectHigher(float? current, float candidate) =>
        current is null || candidate > current ? candidate : current.Value;

    /// <inheritdoc />
    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            try
            {
                _computer?.Close();
            }
            catch
            {
                // Nothing useful to do while shutting down.
            }
            finally
            {
                _computer = null;
                _visitor = null;
            }
        }
    }

    /// <summary>Visitor that causes every hardware node to re-read its sensors.</summary>
    private sealed class UpdateVisitor : IVisitor
    {
        public void VisitComputer(IComputer computer) => computer.Traverse(this);

        public void VisitHardware(IHardware hardware)
        {
            hardware.Update();
            foreach (var sub in hardware.SubHardware)
            {
                sub.Accept(this);
            }
        }

        public void VisitSensor(ISensor sensor)
        {
        }

        public void VisitParameter(IParameter parameter)
        {
        }
    }
}
