using Celsius.Core.Cpu;
using Celsius.Core.Models;
using Xunit;

namespace Celsius.Core.Tests;

public sealed class CpuInfoProviderTests
{
    [Theory]
    [InlineData("12th Gen Intel(R) Core(TM) i7-12700K", CpuVendor.Intel, "i712700k")]
    [InlineData("Intel(R) Core(TM) i9-9900K CPU @ 3.60GHz", CpuVendor.Intel, "i99900k")]
    [InlineData("Intel(R) Core(TM) i5-12400F", CpuVendor.Intel, "i512400f")]
    public void BuildModelKey_Intel_ExtractsModel(string raw, CpuVendor vendor, string expected)
    {
        var key = CpuInfoProvider.BuildModelKey(raw, vendor);
        Assert.Equal(expected, key);
    }

    [Theory]
    [InlineData("AMD Ryzen 7 5800X 8-Core Processor", CpuVendor.Amd, "ryzen75800x")]
    [InlineData("AMD Ryzen 9 7950X 16-Core Processor", CpuVendor.Amd, "ryzen97950x")]
    [InlineData("AMD Ryzen 5 5600X 6-Core Processor", CpuVendor.Amd, "ryzen55600x")]
    public void BuildModelKey_Amd_ExtractsModel(string raw, CpuVendor vendor, string expected)
    {
        var key = CpuInfoProvider.BuildModelKey(raw, vendor);
        Assert.Equal(expected, key);
    }

    [Fact]
    public void BuildModelKey_EmptyName_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, CpuInfoProvider.BuildModelKey("   ", CpuVendor.Intel));
    }
}
