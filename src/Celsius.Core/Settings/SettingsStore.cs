using System.Text.Json;
using Celsius.Core.Settings;

namespace Celsius.Core.Settings;

/// <summary>
/// Loads and saves <see cref="AppSettings"/> as JSON under
/// <c>%AppData%\Celsius\settings.json</c>. All operations are best-effort and
/// never throw to the caller.
/// </summary>
public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    private readonly string _filePath;

    /// <summary>Creates a store using the default per-user location.</summary>
    public SettingsStore()
        : this(Path.Combine(AppPaths.AppDataDirectory, "settings.json"))
    {
    }

    /// <summary>Creates a store using an explicit file path (testable).</summary>
    public SettingsStore(string filePath)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
    }

    /// <summary>Path of the settings file.</summary>
    public string FilePath => _filePath;

    /// <summary>Loads settings, returning defaults when missing or invalid.</summary>
    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_filePath))
            {
                return new AppSettings();
            }

            var json = File.ReadAllText(_filePath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, Options) ?? new AppSettings();
            settings.Clamp();
            return settings;
        }
        catch
        {
            return new AppSettings();
        }
    }

    /// <summary>Saves settings, creating the directory when needed.</summary>
    /// <returns><c>true</c> when the write succeeded.</returns>
    public bool Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        try
        {
            settings.Clamp();
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(settings, Options);
            File.WriteAllText(_filePath, json);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
