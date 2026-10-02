using Celsius.Core.Gpu;
using Celsius.Core.Models;
using Xunit;

namespace Celsius.Core.Tests;

public sealed class GpuInfoProviderTests
{
    [Theory]
    [InlineData(0x10DEu, GpuVendor.Nvidia)]
    [InlineData(0x1002u, GpuVendor.Amd)]
    [InlineData(0x8086u, GpuVendor.Intel)]
    [InlineData(0x1414u, GpuVendor.Microsoft)]
    [InlineData(0x9999u, GpuVendor.Other)]
    [InlineData(0u, GpuVendor.Unknown)]
    public void MapVendor_MapsKnownIds(uint vendorId, GpuVendor expected)
    {
        Assert.Equal(expected, GpuInfoProvider.MapVendor(vendorId));
    }

    [Fact]
    public void IsDiscrete_NvidiaAlwaysDiscrete()
    {
        Assert.True(GpuInfoProvider.IsDiscrete(GpuVendor.Nvidia, 0));
    }

    [Fact]
    public void IsDiscrete_IntelIgpuWithSmallMemoryIsNotDiscrete()
    {
        Assert.False(GpuInfoProvider.IsDiscrete(GpuVendor.Intel, 128UL * 1024 * 1024));
    }

    [Fact]
    public void IsDiscrete_AmdWithLargeMemoryIsDiscrete()
    {
        Assert.True(GpuInfoProvider.IsDiscrete(GpuVendor.Amd, 8UL * 1024 * 1024 * 1024));
    }
}
