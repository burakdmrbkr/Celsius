using Celsius.Core.Models;

namespace Celsius.Core.Stress;

/// <summary>
/// Monitors CPU temperature during a stress run and signals an early abort
/// when the stop threshold is reached. Also accumulates statistics used to
/// build the run summary.
/// </summary>
public sealed class ThermalGuard
{
    private readonly object _sync = new();
    private readonly ThermalProfile _profile;

    private float? _max;
    private float? _min;
    private double _sum;
    private long _count;
    private int _thermalHits;

    /// <summary>Creates a guard for the given thermal profile.</summary>
    public ThermalGuard(ThermalProfile profile)
    {
        _profile = profile ?? throw new ArgumentNullException(nameof(profile));
    }

    /// <summary>The temperature at which the run is aborted.</summary>
    public float StopThresholdC => _profile.StopThresholdC;

    /// <summary>The profile this guard enforces.</summary>
    public ThermalProfile Profile => _profile;

    /// <summary>
    /// Records a temperature sample and returns <c>true</c> when the run must
    /// be aborted because the stop threshold was reached or exceeded.
    /// </summary>
    public bool Sample(float? temperatureC)
    {
        if (temperatureC is not { } value)
        {
            return false;
        }

        lock (_sync)
        {
            _max = _max is null ? value : Math.Max(_max.Value, value);
            _min = _min is null ? value : Math.Min(_min.Value, value);
            _sum += value;
            _count++;

            if (value >= _profile.StopThresholdC)
            {
                _thermalHits++;
                return true;
            }
        }

        return false;
    }

    /// <summary>Builds the summary for a finished run.</summary>
    public StressTestSummary BuildSummary(
        StressTestStopReason reason,
        TimeSpan elapsed,
        TimeSpan requested,
        long totalIterations)
    {
        lock (_sync)
        {
            return new StressTestSummary
            {
                StopReason = reason,
                Elapsed = elapsed,
                RequestedDuration = requested,
                MaxTemperatureC = _max,
                MinTemperatureC = _min,
                AverageTemperatureC = _count > 0 ? (float)(_sum / _count) : null,
                ThermalLimitHits = _thermalHits,
                TotalIterations = totalIterations,
            };
        }
    }
}
