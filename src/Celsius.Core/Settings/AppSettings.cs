using System.Text.Json.Serialization;

namespace Celsius.Core.Settings;

/// <summary>What happens when the main window is closed.</summary>
public enum CloseBehavior
{
    /// <summary>Hide to the system tray; the app keeps running.</summary>
    MinimizeToTray = 0,

    /// <summary>Exit the application entirely.</summary>
    Exit,
}

/// <summary>
/// User-configurable application settings, persisted to
/// <c>%AppData%\Celsius\settings.json</c>.
/// </summary>
public sealed class AppSettings
{
    /// <summary>Sensor polling interval in seconds (0.5 - 10).</summary>
    public double PollIntervalSeconds { get; set; } = 1.0;

    /// <summary>Last stress-test duration used, in minutes (1 - 60).</summary>
    public int LastStressDurationMinutes { get; set; } = 1;

    /// <summary>Worker threads for the stress test. Zero means "all logical processors".</summary>
    public int StressWorkerCount { get; set; }

    /// <summary>What to do when the main window is closed.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CloseBehavior CloseBehavior { get; set; } = CloseBehavior.MinimizeToTray;

    /// <summary>Whether the app should start hidden in the tray.</summary>
    public bool StartMinimized { get; set; }

    /// <summary>
    /// UI culture name (e.g. "en", "tr"), or <c>null</c> to follow the system default.
    /// </summary>
    public string? Language { get; set; }

    /// <summary>Normalizes values into supported ranges.</summary>
    public void Clamp()
    {
        PollIntervalSeconds = Math.Clamp(PollIntervalSeconds, 0.5, 10.0);
        LastStressDurationMinutes = Math.Clamp(LastStressDurationMinutes, 1, 60);
        if (StressWorkerCount < 0)
        {
            StressWorkerCount = 0;
        }

        if (Language is not null && Language is not ("en" or "tr"))
        {
            Language = null;
        }
    }

    /// <summary>Returns a defensive copy.</summary>
    public AppSettings Clone() => new()
    {
        PollIntervalSeconds = PollIntervalSeconds,
        LastStressDurationMinutes = LastStressDurationMinutes,
        StressWorkerCount = StressWorkerCount,
        CloseBehavior = CloseBehavior,
        StartMinimized = StartMinimized,
        Language = Language,
    };
}
