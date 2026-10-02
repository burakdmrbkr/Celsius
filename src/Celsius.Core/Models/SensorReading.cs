namespace Celsius.Core.Models;

/// <summary>
/// A single named sensor reading sampled from the hardware monitoring backend.
/// </summary>
/// <param name="Name">Human readable sensor name, e.g. "CPU Package".</param>
/// <param name="Value">Current value, or <c>null</c> when the sensor is unavailable (e.g. GPU asleep).</param>
/// <param name="Unit">Display unit, e.g. "°C", "%", "MHz".</param>
public readonly record struct SensorReading(string Name, float? Value, string Unit)
{
    /// <summary>Whether this sensor currently produced a usable value.</summary>
    public bool HasValue => Value.HasValue;
}
