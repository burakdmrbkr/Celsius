using System.Globalization;
using System.Resources;

namespace Celsius.App.Localization;

/// <summary>
/// Provides localized strings and supports runtime language switching.
/// </summary>
public static class LocalizationManager
{
    private static readonly ResourceManager Resources =
        new("Celsius.App.Resources.Strings", typeof(LocalizationManager).Assembly);

    /// <summary>Raised after the language changes so views can refresh.</summary>
    public static event EventHandler? LanguageChanged;

    /// <summary>The cultures Celsius ships translations for.</summary>
    public static IReadOnlyList<CultureInfo> SupportedCultures { get; } =
    [
        new CultureInfo("en"),
        new CultureInfo("tr"),
    ];

    /// <summary>Looks up a localized string by key; returns the key when missing.</summary>
    public static string Get(string key) =>
        Resources.GetString(key, CultureInfo.CurrentUICulture) ?? key;

    /// <summary>Applies a culture, or the system default when <paramref name="cultureName"/> is null.</summary>
    public static void Apply(string? cultureName)
    {
        var culture = string.IsNullOrWhiteSpace(cultureName)
            ? CultureInfo.InstalledUICulture
            : new CultureInfo(cultureName);

        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        LanguageChanged?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>The currently applied culture name, or <c>null</c> for system default.</summary>
    public static string? CurrentCultureName { get; private set; }

    /// <summary>Applies a culture and remembers the selection for persistence.</summary>
    public static void ApplyAndRemember(string? cultureName)
    {
        CurrentCultureName = cultureName;
        Apply(cultureName);
    }
}
