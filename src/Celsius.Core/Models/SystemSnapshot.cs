namespace Celsius.Core.Models;

/// <summary>
/// Immutable snapshot of every metric Celsius displays at a point in time.
/// Values are <c>null</c> when unavailable (sensor missing, hardware asleep,
/// or running without elevation).
/// </summary>
public sealed record SystemSnapshot
{
    /// <summary>Timestamp (UTC) at which the snapshot was produced.</summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    // ----- CPU -----
    /// <summary>CPU package / control temperature in °C.</summary>
    public float? CpuTemperatureC { get; init; }

    /// <summary>Current CPU clock in MHz (typically the busiest core).</summary>
    public float? CpuClockMhz { get; init; }

    /// <summary>Total CPU load in percent (0-100).</summary>
    public float? CpuLoadPercent { get; init; }

    // ----- Memory -----
    /// <summary>Used physical memory in percent (0-100).</summary>
    public float? MemoryUsedPercent { get; init; }

    // ----- Storage -----
    /// <summary>Per-drive free-space information.</summary>
    public IReadOnlyList<DriveUsage> Drives { get; init; } = [];

    // ----- GPU -----
    /// <summary>Read-only GPU information (name, temperature, clock).</summary>
    public IReadOnlyList<GpuReading> Gpus { get; init; } = [];

    /// <summary>True when no hardware sensors could be read at all.</summary>
    public bool IsEmpty => CpuTemperatureC is null
        && CpuClockMhz is null
        && CpuLoadPercent is null
        && MemoryUsedPercent is null
        && Drives.Count == 0
        && Gpus.Count == 0;

    /// <summary>An empty snapshot, used as a neutral default.</summary>
    public static SystemSnapshot Empty { get; } = new();
}

/// <summary>Disk usage for a single logical volume / drive.</summary>
/// <param name="Name">Volume label or drive letter, e.g. "C:\".</param>
/// <param name="UsedPercent">Used space in percent (0-100), or <c>null</c> if unknown.</param>
/// <param name="TotalBytes">Total size in bytes, or <c>null</c> if unknown.</param>
public readonly record struct DriveUsage(string Name, float? UsedPercent, long? TotalBytes);

/// <summary>
/// Read-only GPU reading: name plus temperature and clock only.
/// Celsius intentionally does not read VRAM, load, fan or power for GPUs.
/// </summary>
/// <param name="Name">Adapter description, e.g. "NVIDIA GeForce RTX 4070".</param>
/// <param name="Vendor">Detected vendor.</param>
/// <param name="TemperatureC">GPU core temperature in °C, or <c>null</c> when unavailable.</param>
/// <param name="ClockMhz">GPU core clock in MHz, or <c>null</c> when unavailable.</param>
/// <param name="IsDiscrete">True when the adapter is considered a discrete GPU.</param>
public sealed record GpuReading(
    string Name,
    GpuVendor Vendor,
    float? TemperatureC,
    float? ClockMhz,
    bool IsDiscrete);

/// <summary>Known GPU vendors, derived from the PCI vendor id.</summary>
public enum GpuVendor
{
    /// <summary>Vendor could not be determined.</summary>
    Unknown = 0,

    /// <summary>NVIDIA (PCI vendor 0x10DE).</summary>
    Nvidia,

    /// <summary>AMD / ATI (PCI vendor 0x1002).</summary>
    Amd,

    /// <summary>Intel (PCI vendor 0x8086).</summary>
    Intel,

    /// <summary>Microsoft software / WARP adapter (PCI vendor 0x1414).</summary>
    Microsoft,

    /// <summary>Any other vendor.</summary>
    Other,
}
