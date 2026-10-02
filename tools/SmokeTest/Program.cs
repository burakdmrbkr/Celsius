// Temporary smoke test: verifies the stress engine + thermal guard + summary.
using Celsius.Core;
using Celsius.Core.Models;

using var engine = new CelsiusEngine();
engine.Initialize();

var guard = engine.CreateThermalGuard();
var profile = engine.ThermalProfile;
Console.WriteLine($"TjMax={profile.TjMaxC} stop={profile.StopThresholdC} source={profile.Source}");

var options = new StressTestOptions
{
    Duration = TimeSpan.FromMinutes(1),
    WorkerCount = 0,
};

var started = DateTime.UtcNow;
engine.Stressor.Start(options, () =>
{
    var snap = engine.CaptureSnapshot();
    return guard.Sample(snap.CpuTemperatureC);
});

while (engine.Stressor.IsRunning && (DateTime.UtcNow - started).TotalSeconds < 6)
{
    var snap = engine.CaptureSnapshot();
    Console.WriteLine($"  running: t={snap.CpuTemperatureC?.ToString("F1") ?? "-"}C load={snap.CpuLoadPercent?.ToString("F0") ?? "-"}% clock={snap.CpuClockMhz?.ToString("F0") ?? "-"}MHz");
    await Task.Delay(1000);
}

engine.Stressor.Stop();

var reason = await engine.Stressor.WaitForCompletionAsync();
var summary = guard.BuildSummary(reason, DateTime.UtcNow - started, options.Duration, engine.Stressor.Iterations);
Console.WriteLine($"reason={summary.StopReason} elapsed={summary.Elapsed.TotalSeconds:F1}s max={summary.MaxTemperatureC?.ToString("F1") ?? "-"} min={summary.MinTemperatureC?.ToString("F1") ?? "-"} avg={summary.AverageTemperatureC?.ToString("F1") ?? "-"} iters={summary.TotalIterations}");
Console.WriteLine("OK");
