namespace Celsius.Core.Settings;

/// <summary>Well-known per-user application paths.</summary>
public static class AppPaths
{
    /// <summary>Root data folder, <c>%AppData%\Celsius</c>.</summary>
    public static string AppDataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Celsius");

    /// <summary>Log folder, <c>%AppData%\Celsius\logs</c>.</summary>
    public static string LogDirectory { get; } = Path.Combine(AppDataDirectory, "logs");
}
