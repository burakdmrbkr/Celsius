using Celsius.Core.Cpu;
using Celsius.Core.Logging;
using Celsius.Core.Models;
using Celsius.Core.Settings;
using Celsius.Core.Stress;
using Celsius.Core.Thermal;

namespace Celsius.Core;

/// <summary>
/// High-level facade that composes the hardware monitor, thermal resolution,
/// stress engine and settings into a single object the UI can drive.
/// </summary>
public sealed class CelsiusEngine : IDisposable
{
    private readonly IHardwareMonitor _monitor;
    private readonly CpuInfoProvider _cpuInfoProvider;
    private readonly ThermalThresholdResolver _thermalResolver;
    private readonly FileLogger _logger;

    private bool _disposed;

    /// <summary>Creates an engine with default production dependencies.</summary>
    public CelsiusEngine()
        : this(new HardwareMonitorService(), new CpuInfoProvider(), new ThermalThresholdResolver(), new FileLogger())
    {
    }

    /// <summary>Creates an engine with explicit dependencies (testable).</summary>
    public CelsiusEngine(
        IHardwareMonitor monitor,
        CpuInfoProvider cpuInfoProvider,
        ThermalThresholdResolver thermalResolver,
        FileLogger logger)
    {
        _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        _cpuInfoProvider = cpuInfoProvider ?? throw new ArgumentNullException(nameof(cpuInfoProvider));
        _thermalResolver = thermalResolver ?? throw new ArgumentNullException(nameof(thermalResolver));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Stressor = new StressorService();
        Settings = new SettingsStore();
    }

    /// <summary>The stress-test engine.</summary>
    public StressorService Stressor { get; }

    /// <summary>Persisted application settings accessor.</summary>
    public SettingsStore Settings { get; }

    /// <summary>Detected CPU identity, available after <see cref="Initialize"/>.</summary>
    public CpuInfo Cpu { get; private set; } = new("", CpuVendor.Unknown, string.Empty);

    /// <summary>Resolved thermal profile, available after <see cref="Initialize"/>.</summary>
    public ThermalProfile ThermalProfile { get; private set; } =
        ThermalThresholdResolver.CreateProfile(
            ThermalProfileDefaults.FallbackTjMaxC,
            ThermalSource.Default,
            null);

    /// <summary>True when the backend produced at least one temperature sensor.</summary>
    public bool HasTemperatureSensors => _monitor.HasTemperatureSensors;

    /// <summary>
    /// Reason why temperatures may be unavailable (e.g. the PawnIO kernel driver
    /// could not be loaded), or <c>null</c> when the backend is healthy.
    /// </summary>
    public string? SensorError => _monitor.LastError;

    /// <summary>
    /// True when the backend initialized but no temperature sensor produced a
    /// value — the classic symptom of a missing/blocked kernel driver.
    /// </summary>
    public bool TemperaturesUnavailable => !HasTemperatureSensors;

    /// <summary>Initializes hardware access and resolves the thermal profile.</summary>
    public void Initialize()
    {
        try
        {
            _monitor.Initialize();
        }
        catch (Exception ex)
        {
            _logger.Critical("Failed to initialize hardware monitor.", ex);
        }

        try
        {
            Cpu = _cpuInfoProvider.GetCpuInfo();
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to detect CPU information.", ex);
        }

        var hardwareTjMax = _monitor.TryGetTjMaxC();
        ThermalProfile = _thermalResolver.Resolve(Cpu, hardwareTjMax);
    }

    /// <summary>Refreshes and captures a snapshot. Call from a background thread.</summary>
    public SystemSnapshot CaptureSnapshot()
    {
        try
        {
            _monitor.Refresh();
            return _monitor.Capture();
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to capture system snapshot.", ex);
            return SystemSnapshot.Empty;
        }
    }

    /// <summary>
    /// Builds a thermal guard bound to the current profile, for use by a stress run.
    /// </summary>
    public ThermalGuard CreateThermalGuard() => new(ThermalProfile);

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _monitor.Dispose();
    }
}
