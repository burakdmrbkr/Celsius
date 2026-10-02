using Celsius.Core.Models;
using Celsius.Core.Thermal;
using Celsius.Core.Stress;
using Xunit;

namespace Celsius.Core.Tests;

public sealed class ThermalGuardTests
{
    private static ThermalGuard CreateGuard(float tjMax = 100)
    {
        var profile = ThermalThresholdResolver.CreateProfile(tjMax, ThermalSource.Hardware, "test");
        return new ThermalGuard(profile);
    }

    [Fact]
    public void Sample_ReturnsFalse_WhenBelowThreshold()
    {
        var guard = CreateGuard();
        Assert.False(guard.Sample(80));
    }

    [Fact]
    public void Sample_ReturnsTrue_WhenAtOrAboveThreshold()
    {
        var guard = CreateGuard(100); // stop threshold = 95
        Assert.True(guard.Sample(95));
        Assert.True(guard.Sample(99));
    }

    [Fact]
    public void Sample_IgnoresNullValues()
    {
        var guard = CreateGuard();
        Assert.False(guard.Sample(null));
    }

    [Fact]
    public void BuildSummary_ComputesStatistics()
    {
        var guard = CreateGuard();
        guard.Sample(70);
        guard.Sample(80);
        guard.Sample(90);

        var summary = guard.BuildSummary(
            StressTestStopReason.Completed,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(1),
            totalIterations: 12345);

        Assert.Equal(90, summary.MaxTemperatureC);
        Assert.Equal(70, summary.MinTemperatureC);
        Assert.Equal(80, summary.AverageTemperatureC);
        Assert.Equal(12345, summary.TotalIterations);
        Assert.Equal(StressTestStopReason.Completed, summary.StopReason);
    }

    [Fact]
    public void BuildSummary_CountsThermalHits()
    {
        var guard = CreateGuard(100); // threshold 95
        guard.Sample(96);
        guard.Sample(97);
        guard.Sample(50);

        var summary = guard.BuildSummary(
            StressTestStopReason.ThermalLimitExceeded,
            TimeSpan.FromSeconds(30),
            TimeSpan.FromMinutes(1),
            totalIterations: 0);

        Assert.Equal(2, summary.ThermalLimitHits);
    }

    [Fact]
    public void BuildSummary_NoSamples_ReturnsNullStatistics()
    {
        var guard = CreateGuard();

        var summary = guard.BuildSummary(
            StressTestStopReason.UserStopped,
            TimeSpan.Zero,
            TimeSpan.FromMinutes(1),
            0);

        Assert.Null(summary.MaxTemperatureC);
        Assert.Null(summary.MinTemperatureC);
        Assert.Null(summary.AverageTemperatureC);
    }
}
