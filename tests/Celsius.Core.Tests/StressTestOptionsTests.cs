using Celsius.Core.Models;
using Xunit;

namespace Celsius.Core.Tests;

public sealed class StressTestOptionsTests
{
    [Fact]
    public void Validate_AcceptsValidDuration()
    {
        var options = new StressTestOptions { Duration = TimeSpan.FromMinutes(5) };
        options.Validate(); // should not throw
    }

    [Theory]
    [InlineData(0)]
    [InlineData(61)]
    [InlineData(-1)]
    public void Validate_RejectsOutOfRangeDuration(int minutes)
    {
        var options = new StressTestOptions { Duration = TimeSpan.FromMinutes(minutes) };
        Assert.Throws<ArgumentOutOfRangeException>(() => options.Validate());
    }

    [Fact]
    public void Validate_RejectsNegativeWorkerCount()
    {
        var options = new StressTestOptions
        {
            Duration = TimeSpan.FromMinutes(1),
            WorkerCount = -1,
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => options.Validate());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(60)]
    public void Validate_AcceptsPresetDurations(int minutes)
    {
        var options = new StressTestOptions { Duration = TimeSpan.FromMinutes(minutes) };
        options.Validate();
    }
}
