using Celsius.Core.Models;
using Celsius.Core.Thermal;
using Xunit;

namespace Celsius.Core.Tests;

public sealed class ThermalThresholdResolverTests
{
    private static ThermalThresholdResolver CreateResolver() => new(
        new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase)
        {
            ["i712700k"] = 100,
            ["ryzen75800x"] = 90,
        });

    [Fact]
    public void Resolve_PrefersHardwareValue_OverLookup()
    {
        var resolver = CreateResolver();
        var cpu = new CpuInfo("Intel Core i7-12700K", CpuVendor.Intel, "i712700k");

        var profile = resolver.Resolve(cpu, hardwareTjMaxC: 105);

        Assert.Equal(105, profile.TjMaxC);
        Assert.Equal(ThermalSource.Hardware, profile.Source);
    }

    [Fact]
    public void Resolve_UsesLookup_WhenHardwareMissing()
    {
        var resolver = CreateResolver();
        var cpu = new CpuInfo("AMD Ryzen 7 5800X", CpuVendor.Amd, "ryzen75800x");

        var profile = resolver.Resolve(cpu, hardwareTjMaxC: null);

        Assert.Equal(90, profile.TjMaxC);
        Assert.Equal(ThermalSource.LookupTable, profile.Source);
    }

    [Fact]
    public void Resolve_FallsBackToDefault_ForUnknownModel()
    {
        var resolver = CreateResolver();
        var cpu = new CpuInfo("Some Unknown CPU", CpuVendor.Unknown, "unknow1234");

        var profile = resolver.Resolve(cpu, hardwareTjMaxC: null);

        Assert.Equal(ThermalProfileDefaults.FallbackTjMaxC, profile.TjMaxC);
        Assert.Equal(ThermalSource.Default, profile.Source);
    }

    [Theory]
    [InlineData(10f)]
    [InlineData(200f)]
    [InlineData(float.NaN)]
    public void Resolve_RejectsImplausibleHardwareValue(float hardwareTjMax)
    {
        var resolver = CreateResolver();
        var cpu = new CpuInfo("Intel Core i7-12700K", CpuVendor.Intel, "i712700k");

        var profile = resolver.Resolve(cpu, hardwareTjMaxC: hardwareTjMax);

        Assert.Equal(ThermalSource.LookupTable, profile.Source);
    }

    [Fact]
    public void CreateProfile_AppliesFiveDegreeMargin()
    {
        var profile = ThermalThresholdResolver.CreateProfile(100, ThermalSource.Hardware, "x");
        Assert.Equal(95, profile.StopThresholdC);
        Assert.Equal(5, profile.MarginC);
    }

    [Fact]
    public void CreateProfile_ClampsStopThresholdToFloor()
    {
        var profile = ThermalThresholdResolver.CreateProfile(72, ThermalSource.Hardware, "x");

        Assert.Equal(ThermalProfileDefaults.MinStopThresholdC, profile.StopThresholdC);
    }

    [Fact]
    public void BundledLookup_ContainsKnownModels()
    {
        // Uses the real embedded resource.
        var resolver = new ThermalThresholdResolver();
        var cpu = new CpuInfo("AMD Ryzen 7 5800X", CpuVendor.Amd, "ryzen75800x");

        var profile = resolver.Resolve(cpu, hardwareTjMaxC: null);

        Assert.Equal(ThermalSource.LookupTable, profile.Source);
        Assert.Equal(90, profile.TjMaxC);
    }
}
