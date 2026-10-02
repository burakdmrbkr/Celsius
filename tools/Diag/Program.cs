// Diagnostic: reports CPU identity, thermal profile and whether temperature
// sensors (which require the PawnIO kernel driver) are actually available.
using Celsius.Core;

var elevated = IsElevated();
Console.WriteLine($"Elevated       : {elevated}");

using var engine = new CelsiusEngine();
engine.Initialize();

Console.WriteLine($"CPU            : {engine.Cpu.Name}");
Console.WriteLine($"Vendor         : {engine.Cpu.Vendor}");
Console.WriteLine($"ModelKey       : {engine.Cpu.ModelKey}");
Console.WriteLine($"TjMax          : {engine.ThermalProfile.TjMaxC} C (source: {engine.ThermalProfile.Source})");
Console.WriteLine($"StopThreshold  : {engine.ThermalProfile.StopThresholdC} C");
Console.WriteLine($"HasTempSensors : {engine.HasTemperatureSensors}");
Console.WriteLine($"SensorError    : {engine.SensorError ?? "(none)"}");

Console.WriteLine();
Console.WriteLine("Snapshot:");
for (var i = 0; i < 3; i++)
{
    var snap = engine.CaptureSnapshot();
    Console.WriteLine(
        $"  cpuTemp={Fmt(snap.CpuTemperatureC)}C " +
        $"cpuClock={Fmt(snap.CpuClockMhz)}MHz " +
        $"cpuLoad={Fmt(snap.CpuLoadPercent)}% " +
        $"mem={Fmt(snap.MemoryUsedPercent)}% " +
        $"drives={snap.Drives.Count} gpus={snap.Gpus.Count}");

    foreach (var gpu in snap.Gpus)
    {
        Console.WriteLine(
            $"    GPU: {gpu.Name} | vendor={gpu.Vendor} discrete={gpu.IsDiscrete} " +
            $"temp={Fmt(gpu.TemperatureC)}C clock={Fmt(gpu.ClockMhz)}MHz");
    }

    Thread.Sleep(700);
}

Console.WriteLine();
Console.WriteLine(engine.HasTemperatureSensors
    ? "RESULT: temperatures OK."
    : "RESULT: NO temperature sensors -> PawnIO driver likely missing/blocked (or not elevated).");

static string Fmt(float? v) => v is { } f ? f.ToString("F1") : "-";

static bool IsElevated()
{
    using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
    var principal = new System.Security.Principal.WindowsPrincipal(identity);
    return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
}
