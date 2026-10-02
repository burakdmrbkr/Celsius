namespace Celsius.Core.Models;

/// <summary>
/// The thermal specification resolved for the detected CPU, together with the
/// derived safety limit used by the stress test.
/// </summary>
public sealed record ThermalProfile
{
    /// <summary>Maximum junction temperature (TjMax) in °C as published by the vendor.</summary>
    public required float TjMaxC { get; init; }

    /// <summary>Fixed safety margin in °C subtracted from <see cref="TjMaxC"/>.</summary>
    public float MarginC { get; init; } = ThermalProfileDefaults.MarginC;

    /// <summary>Temperature at which the stress test is automatically stopped.</summary>
    public float StopThresholdC { get; init; }

    /// <summary>Where the TjMax value came from, for diagnostics.</summary>
    public required ThermalSource Source { get; init; }

    /// <summary>The CPU model key this profile was resolved for.</summary>
    public string? ModelKey { get; init; }
}

/// <summary>Source of a resolved TjMax value, ordered by trustworthiness.</summary>
public enum ThermalSource
{
    /// <summary>Conservative built-in default, used when nothing else worked.</summary>
    Default = 0,

    /// <summary>Resolved from the bundled CPU lookup table.</summary>
    LookupTable = 1,

    /// <summary>Reported by the hardware itself (e.g. LHM TjMax sensor parameter).</summary>
    Hardware = 2,
}

/// <summary>Constants governing the thermal safety margin and clamping.</summary>
public static class ThermalProfileDefaults
{
    /// <summary>Fixed safety margin in °C subtracted from TjMax.</summary>
    public const float MarginC = 5f;

    /// <summary>Conservative TjMax fallback in °C.</summary>
    public const float FallbackTjMaxC = 90f;

    /// <summary>The stop threshold is never allowed below this temperature.</summary>
    public const float MinStopThresholdC = 70f;
}
