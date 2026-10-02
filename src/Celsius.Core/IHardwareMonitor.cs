using Celsius.Core.Models;

namespace Celsius.Core;

/// <summary>
/// Abstraction over the hardware monitoring backend (LibreHardwareMonitor).
/// Implementations are expected to be safe to call concurrently and to be
/// disposed when the application shuts down.
/// </summary>
public interface IHardwareMonitor : IDisposable
{
    /// <summary>
    /// Opens the underlying hardware backend and performs initial discovery.
    /// Safe to call multiple times; subsequent calls are no-ops.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Causes the backend to re-read all enabled sensors. Call this from a
    /// background thread — it is comparatively expensive.
    /// </summary>
    void Refresh();

    /// <summary>
    /// Produces an immutable snapshot of every metric Celsius displays.
    /// </summary>
    SystemSnapshot Capture();

    /// <summary>
    /// Attempts to read the hardware-reported TjMax (maximum junction
    /// temperature) in °C. Returns <c>null</c> when the backend cannot provide it.
    /// </summary>
    float? TryGetTjMaxC();

    /// <summary>True when at least one temperature sensor could be read.</summary>
    bool HasTemperatureSensors { get; }
}
