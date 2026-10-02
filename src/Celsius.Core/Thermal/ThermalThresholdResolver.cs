using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Celsius.Core.Models;

namespace Celsius.Core.Thermal;

/// <summary>
/// Resolves the CPU's maximum junction temperature (TjMax) and derives the
/// safety stop threshold used by the stress test.
/// </summary>
/// <remarks>
/// Resolution order, most trustworthy first:
/// <list type="number">
///   <item>The value reported by the hardware itself (LHM sensor parameter).</item>
///   <item>The bundled per-model lookup table.</item>
///   <item>A conservative default (<see cref="ThermalProfileDefaults.FallbackTjMaxC"/>).</item>
/// </list>
/// </remarks>
public sealed class ThermalThresholdResolver
{
    private const string ResourceName = "Celsius.Core.Resources.cpu-tjmax.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly Dictionary<string, float> _lookup;
    private readonly float _fallbackTjMaxC;

    /// <summary>Creates a resolver using the bundled lookup table.</summary>
    public ThermalThresholdResolver()
        : this(LoadBundledLookup(), ThermalProfileDefaults.FallbackTjMaxC)
    {
    }

    /// <summary>Creates a resolver with an explicit lookup and fallback (testable).</summary>
    /// <param name="lookup">Model key to TjMax map.</param>
    /// <param name="fallbackTjMaxC">TjMax to use when nothing else resolves.</param>
    public ThermalThresholdResolver(
        IReadOnlyDictionary<string, float> lookup,
        float fallbackTjMaxC = ThermalProfileDefaults.FallbackTjMaxC)
    {
        ArgumentNullException.ThrowIfNull(lookup);

        _lookup = new Dictionary<string, float>(
            lookup.ToDictionary(
                kvp => NormalizeKey(kvp.Key),
                kvp => kvp.Value,
                StringComparer.OrdinalIgnoreCase),
            StringComparer.OrdinalIgnoreCase);

        _fallbackTjMaxC = fallbackTjMaxC;
    }

    /// <summary>
    /// Resolves the thermal profile for the given CPU, preferring the
    /// hardware-reported value when it is plausible.
    /// </summary>
    /// <param name="cpu">Detected CPU identity (may be null).</param>
    /// <param name="hardwareTjMaxC">TjMax reported by the hardware, if any.</param>
    public ThermalProfile Resolve(CpuInfo? cpu, float? hardwareTjMaxC)
    {
        float tjMax;
        ThermalSource source;

        if (IsPlausible(hardwareTjMaxC))
        {
            tjMax = hardwareTjMaxC!.Value;
            source = ThermalSource.Hardware;
        }
        else if (cpu is not null
            && cpu.ModelKey.Length > 0
            && _lookup.TryGetValue(NormalizeKey(cpu.ModelKey), out var fromTable))
        {
            tjMax = fromTable;
            source = ThermalSource.LookupTable;
        }
        else
        {
            tjMax = _fallbackTjMaxC;
            source = ThermalSource.Default;
        }

        return CreateProfile(tjMax, source, cpu?.ModelKey);
    }

    /// <summary>Builds a profile, applying the fixed margin and clamping.</summary>
    public static ThermalProfile CreateProfile(
        float tjMaxC,
        ThermalSource source,
        string? modelKey)
    {
        var stop = tjMaxC - ThermalProfileDefaults.MarginC;

        // Never allow the stop threshold below the floor or above TjMax.
        if (stop < ThermalProfileDefaults.MinStopThresholdC)
        {
            stop = Math.Min(ThermalProfileDefaults.MinStopThresholdC, tjMaxC);
        }

        if (stop > tjMaxC)
        {
            stop = tjMaxC;
        }

        return new ThermalProfile
        {
            TjMaxC = tjMaxC,
            MarginC = ThermalProfileDefaults.MarginC,
            StopThresholdC = stop,
            Source = source,
            ModelKey = modelKey,
        };
    }

    private static bool IsPlausible(float? value)
    {
        // Real CPU TjMax values live in roughly 60-125 C. Reject obvious nonsense.
        return value is >= 60f and <= 125f;
    }

    private static string NormalizeKey(string key) =>
        key.Replace("-", string.Empty, StringComparison.Ordinal)
           .Replace(" ", string.Empty, StringComparison.Ordinal)
           .Trim()
           .ToLowerInvariant();

    private static Dictionary<string, float> LoadBundledLookup()
    {
        var result = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        try
        {
            using var stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream(ResourceName);
            if (stream is null)
            {
                return result;
            }

            var table = JsonSerializer.Deserialize<CpuTjMaxTable>(stream, SerializerOptions);
            if (table is null)
            {
                return result;
            }

            MergeInto(result, table.Intel);
            MergeInto(result, table.Amd);
        }
        catch
        {
            // A corrupt or missing resource must never crash startup; the
            // resolver simply falls back to the default TjMax.
        }

        return result;
    }

    private static void MergeInto(Dictionary<string, float> target, Dictionary<string, float>? source)
    {
        if (source is null)
        {
            return;
        }

        foreach (var (key, value) in source)
        {
            if (key.StartsWith('$'))
            {
                continue;
            }

            target[NormalizeKey(key)] = value;
        }
    }

    private sealed class CpuTjMaxTable
    {
        [JsonPropertyName("intel")]
        public Dictionary<string, float>? Intel { get; set; }

        [JsonPropertyName("amd")]
        public Dictionary<string, float>? Amd { get; set; }
    }
}
