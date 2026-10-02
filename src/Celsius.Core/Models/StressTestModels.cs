namespace Celsius.Core.Models;

/// <summary>
/// Options controlling a single CPU stress test run.
/// </summary>
public sealed record StressTestOptions
{
    /// <summary>Minimum selectable duration in minutes.</summary>
    public const int MinDurationMinutes = 1;

    /// <summary>Maximum selectable duration in minutes.</summary>
    public const int MaxDurationMinutes = 60;

    /// <summary>Duration of the run.</summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>Number of worker threads. Zero means "one per logical processor".</summary>
    public int WorkerCount { get; init; }

    /// <summary>Validates the options, throwing when out of range.</summary>
    public void Validate()
    {
        var minutes = Duration.TotalMinutes;
        if (minutes < MinDurationMinutes || minutes > MaxDurationMinutes)
        {
            throw new ArgumentOutOfRangeException(
                nameof(Duration),
                Duration,
                $"Duration must be between {MinDurationMinutes} and {MaxDurationMinutes} minutes.");
        }

        if (WorkerCount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(WorkerCount),
                WorkerCount,
                "Worker count cannot be negative.");
        }
    }
}

/// <summary>Reason a stress test ended.</summary>
public enum StressTestStopReason
{
    /// <summary>The requested duration elapsed normally.</summary>
    Completed = 0,

    /// <summary>The user pressed Stop.</summary>
    UserStopped,

    /// <summary>The thermal guard stopped the run because the limit was exceeded.</summary>
    ThermalLimitExceeded,

    /// <summary>The run ended because of an unexpected error.</summary>
    Error,
}

/// <summary>
/// Summary produced when a stress test finishes, shown to the user afterwards.
/// </summary>
public sealed record StressTestSummary
{
    /// <summary>Why the run ended.</summary>
    public required StressTestStopReason StopReason { get; init; }

    /// <summary>Wall-clock duration the load actually ran.</summary>
    public required TimeSpan Elapsed { get; init; }

    /// <summary>Requested duration for reference.</summary>
    public required TimeSpan RequestedDuration { get; init; }

    /// <summary>Highest CPU temperature observed in °C, if any.</summary>
    public float? MaxTemperatureC { get; init; }

    /// <summary>Lowest CPU temperature observed in °C, if any.</summary>
    public float? MinTemperatureC { get; init; }

    /// <summary>Average CPU temperature observed in °C, if any.</summary>
    public float? AverageTemperatureC { get; init; }

    /// <summary>Number of samples that exceeded the stop threshold.</summary>
    public int ThermalLimitHits { get; init; }

    /// <summary>Total work iterations completed across all workers.</summary>
    public long TotalIterations { get; init; }
}
