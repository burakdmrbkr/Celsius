using System.Diagnostics;
using Celsius.Core.Models;

namespace Celsius.Core.Stress;

/// <summary>
/// Runs a CPU stress load for a bounded duration across a configurable number
/// of worker threads. The load is a floating-point heavy loop designed to push
/// the cores (and therefore the temperature) up.
/// </summary>
public sealed class StressorService
{
    private readonly object _sync = new();
    private CancellationTokenSource? _cts;
    private Task<StressTestStopReason>? _runTask;
    private long _iterations;

    /// <summary>Raised roughly once per second with the elapsed time while running.</summary>
    public event EventHandler<TimeSpan>? Progress;

    /// <summary>Raised when the run ends, with the reason it stopped.</summary>
    public event EventHandler<StressTestStopReason>? Stopped;

    /// <summary>True while a stress run is active.</summary>
    public bool IsRunning
    {
        get
        {
            lock (_sync)
            {
                return _runTask is { IsCompleted: false };
            }
        }
    }

    /// <summary>Total work iterations completed during the last run.</summary>
    public long Iterations => Interlocked.Read(ref _iterations);

    /// <summary>
    /// Starts a stress run. Throws when a run is already active.
    /// </summary>
    /// <param name="options">Run options; validated before starting.</param>
    /// <param name="onThermalAbort">
    /// Called periodically; return <c>true</c> to stop the run because the
    /// thermal limit was exceeded.
    /// </param>
    public void Start(StressTestOptions options, Func<bool>? onThermalAbort = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        lock (_sync)
        {
            if (_runTask is { IsCompleted: false })
            {
                throw new InvalidOperationException("A stress test is already running.");
            }

            Interlocked.Exchange(ref _iterations, 0);
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            var workers = options.WorkerCount > 0
                ? options.WorkerCount
                : Environment.ProcessorCount;

            _runTask = Task.Run(
                () => RunAsync(workers, options.Duration, onThermalAbort, token),
                CancellationToken.None);
        }
    }

    /// <summary>Requests the running test to stop. No-op when not running.</summary>
    public void Stop()
    {
        lock (_sync)
        {
            _cts?.Cancel();
        }
    }

    /// <summary>
    /// Awaits the completion of the current run and returns the reason it stopped.
    /// </summary>
    public async Task<StressTestStopReason> WaitForCompletionAsync()
    {
        Task<StressTestStopReason>? task;
        lock (_sync)
        {
            task = _runTask;
        }

        if (task is null)
        {
            return StressTestStopReason.Completed;
        }

        var reason = await task.ConfigureAwait(false);
        return reason;
    }

    private async Task<StressTestStopReason> RunAsync(
        int workers,
        TimeSpan duration,
        Func<bool>? onThermalAbort,
        CancellationToken token)
    {
        var reason = StressTestStopReason.Completed;
        var sw = Stopwatch.StartNew();
        var deadline = duration;

        using var progressTimer = new System.Threading.Timer(
            _ => Progress?.Invoke(this, sw.Elapsed),
            null,
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(1));

        try
        {
            var workerTasks = new Task[workers];
            for (var i = 0; i < workers; i++)
            {
                workerTasks[i] = Task.Run(
                    () => BurnLoop(deadline, onThermalAbort, token),
                    CancellationToken.None);
            }

            await Task.WhenAll(workerTasks).ConfigureAwait(false);

            reason = token.IsCancellationRequested
                ? StressTestStopReason.UserStopped
                : StressTestStopReason.Completed;
        }
        catch (AggregateException)
        {
            reason = StressTestStopReason.Error;
        }
        catch (Exception)
        {
            reason = StressTestStopReason.Error;
        }

        Stopped?.Invoke(this, reason);
        return reason;
    }

    private void BurnLoop(TimeSpan duration, Func<bool>? onThermalAbort, CancellationToken token)
    {
        var sw = Stopwatch.StartNew();
        double accumulator = 1.000001;
        var lastThermalCheck = TimeSpan.Zero;

        while (sw.Elapsed < duration && !token.IsCancellationRequested)
        {
            // A batch of floating-point work keeps the core busy between checks.
            for (var i = 0; i < 100_000; i++)
            {
                accumulator = Math.Sqrt(accumulator) + Math.Sin(accumulator) * 1.0000001;
                if (accumulator > 1e6)
                {
                    accumulator = 1.000001;
                }
            }

            Interlocked.Add(ref _iterations, 100_000);

            // Poll the thermal guard at most once per second per worker.
            if (onThermalAbort is not null
                && (sw.Elapsed - lastThermalCheck) >= TimeSpan.FromSeconds(1))
            {
                lastThermalCheck = sw.Elapsed;
                if (onThermalAbort())
                {
                    _cts?.Cancel();
                    return;
                }
            }
        }

        // Prevent the JIT from optimizing the loop away.
        Volatile.Write(ref _sink, accumulator);
    }

    private static double _sink;
}
