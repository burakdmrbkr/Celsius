using System.Management;
using System.Text.RegularExpressions;
using Celsius.Core.Models;
using Microsoft.Win32;

namespace Celsius.Core.Cpu;

/// <summary>
/// Detects the installed CPU and produces a normalized model key that can be
/// matched against the bundled TjMax lookup table.
/// </summary>
/// <remarks>
/// Detection is best-effort and never throws: if every source fails an
/// <see cref="CpuVendor.Unknown"/> result with an empty model key is returned.
/// </remarks>
public sealed partial class CpuInfoProvider
{
    private const string ProcessorKeyPath =
        @"HARDWARE\DESCRIPTION\System\CentralProcessor\0";

    /// <summary>Reads the CPU identity using WMI, falling back to the registry.</summary>
    public CpuInfo GetCpuInfo()
    {
        var (name, manufacturer) = TryQueryWmi();

        name ??= TryReadRegistryName();
        name ??= string.Empty;

        var vendor = DetectVendor(name, manufacturer);
        var modelKey = BuildModelKey(name, vendor);

        return new CpuInfo(name.Trim(), vendor, modelKey);
    }

    private static (string? Name, string? Manufacturer) TryQueryWmi()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(
                "SELECT Name, Manufacturer FROM Win32_Processor");
            using var results = searcher.Get();

            foreach (ManagementBaseObject item in results)
            {
                using (item)
                {
                    var name = item["Name"] as string;
                    var manufacturer = item["Manufacturer"] as string;
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        return (name, manufacturer);
                    }
                }
            }
        }
        catch
        {
            // WMI can be unavailable or blocked; fall through to the registry.
        }

        return (null, null);
    }

    private static string? TryReadRegistryName()
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(ProcessorKeyPath);
            return key?.GetValue("ProcessorNameString") as string;
        }
        catch
        {
            return null;
        }
    }

    private static CpuVendor DetectVendor(string name, string? manufacturer)
    {
        var haystack = $"{manufacturer} {name}".ToUpperInvariant();
        if (haystack.Contains("INTEL") || haystack.Contains("GENUINEINTEL"))
        {
            return CpuVendor.Intel;
        }

        if (haystack.Contains("AMD") || haystack.Contains("AUTHENTICAMD"))
        {
            return CpuVendor.Amd;
        }

        return CpuVendor.Unknown;
    }

    /// <summary>
    /// Builds a lowercase, whitespace-free model token such as
    /// <c>i7-12700k</c> or <c>ryzen75800x</c>, stripping marketing noise.
    /// </summary>
    internal static string BuildModelKey(string rawName, CpuVendor vendor)
    {
        if (string.IsNullOrWhiteSpace(rawName))
        {
            return string.Empty;
        }

        var text = rawName;

        // Drop common prefixes/suffixes first.
        text = PrefixNoiseRegex().Replace(text, " ");
        text = ClockSuffixRegex().Replace(text, " ");
        text = CoreCountSuffixRegex().Replace(text, " ");
        text = TradeMarkRegex().Replace(text, string.Empty);

        // Keep only model-relevant alphanumerics.
        var tokens = TokenRegex().Matches(text)
            .Select(m => m.Value)
            .Where(t => t.Length > 0)
            .ToList();

        if (tokens.Count == 0)
        {
            return string.Empty;
        }

        // For AMD prefer the "ryzen" + numeric token; for Intel prefer the
        // token that looks like a model number (contains a digit and a dash).
        var key = vendor switch
        {
            CpuVendor.Amd => SelectAmdToken(tokens),
            CpuVendor.Intel => SelectIntelToken(tokens),
            _ => tokens[^1],
        };

        return key.Replace("-", string.Empty, StringComparison.Ordinal).ToLowerInvariant();
    }

    private static string SelectAmdToken(List<string> tokens)
    {
        var index = tokens.FindIndex(t => t.Equals("ryzen", StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
        {
            // "ryzen 7 5800x" -> tier digit at index+1, model at index+2.
            // "ryzen 7" (Threadripper-style naming) -> model at index+1.
            if (index + 2 < tokens.Count && tokens[index + 1].All(char.IsDigit))
            {
                return "ryzen" + tokens[index + 1] + tokens[index + 2];
            }

            if (index + 1 < tokens.Count)
            {
                return "ryzen" + tokens[index + 1];
            }
        }

        // Fall back to the longest numeric-looking token.
        return tokens
            .Where(t => t.Any(char.IsDigit))
            .OrderByDescending(t => t.Length)
            .FirstOrDefault() ?? tokens[^1];
    }

    private static string SelectIntelToken(List<string> tokens)
    {
        // Prefer tokens like "i7-12700K", "i5-12400F", "ultra-7-155H".
        var candidate = tokens.FirstOrDefault(t =>
            ModelTokenRegex().IsMatch(t));
        if (candidate is not null)
        {
            return candidate;
        }

        var ultraIndex = tokens.FindIndex(t =>
            t.Equals("ultra", StringComparison.OrdinalIgnoreCase));
        if (ultraIndex >= 0 && ultraIndex + 2 < tokens.Count)
        {
            return tokens[ultraIndex + 1] + tokens[ultraIndex + 2];
        }

        return tokens.FirstOrDefault(t => t.Any(char.IsDigit)) ?? tokens[^1];
    }

    [GeneratedRegex(@"\b(\d{1,2}(st|nd|rd|th)\s+gen|core\(tm\)|core\b|cpu)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex PrefixNoiseRegex();

    [GeneratedRegex(@"@\s*[\d.]+\s*ghz", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex ClockSuffixRegex();

    [GeneratedRegex(@"\b(\d+|dual|quad|hexa|octa|deca)[- ]core\b(\s+processor)?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex CoreCountSuffixRegex();

    [GeneratedRegex(@"\(tm\)|\(r\)|\(c\)|®|™", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex TradeMarkRegex();

    [GeneratedRegex(@"[A-Za-z0-9\-]+", RegexOptions.Compiled)]
    private static partial Regex TokenRegex();

    [GeneratedRegex(@"^(i[3579]|ultra[\- ]?[3579]|n\d{2,4}|g\d{4})[\-]?\w+$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex ModelTokenRegex();
}
