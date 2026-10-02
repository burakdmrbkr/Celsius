using Celsius.Core.Settings;
using Xunit;

namespace Celsius.Core.Tests;

public sealed class SettingsStoreTests
{
    [Fact]
    public void SaveAndLoad_RoundTrips()
    {
        var path = Path.Combine(Path.GetTempPath(), "celsius-tests", Guid.NewGuid() + ".json");
        try
        {
            var store = new SettingsStore(path);
            var settings = new AppSettings
            {
                PollIntervalSeconds = 2.5,
                LastStressDurationMinutes = 10,
                StressWorkerCount = 4,
                CloseBehavior = CloseBehavior.Exit,
                StartMinimized = true,
            };

            Assert.True(store.Save(settings));
            var loaded = store.Load();

            Assert.Equal(2.5, loaded.PollIntervalSeconds);
            Assert.Equal(10, loaded.LastStressDurationMinutes);
            Assert.Equal(4, loaded.StressWorkerCount);
            Assert.Equal(CloseBehavior.Exit, loaded.CloseBehavior);
            Assert.True(loaded.StartMinimized);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void Load_MissingFile_ReturnsDefaults()
    {
        var path = Path.Combine(Path.GetTempPath(), "celsius-tests", Guid.NewGuid() + ".json");
        var store = new SettingsStore(path);

        var loaded = store.Load();

        Assert.Equal(1.0, loaded.PollIntervalSeconds);
        Assert.Equal(CloseBehavior.MinimizeToTray, loaded.CloseBehavior);
    }

    [Fact]
    public void Clamp_EnforcesRanges()
    {
        var settings = new AppSettings
        {
            PollIntervalSeconds = 999,
            LastStressDurationMinutes = 500,
            StressWorkerCount = -5,
        };

        settings.Clamp();

        Assert.Equal(10.0, settings.PollIntervalSeconds);
        Assert.Equal(60, settings.LastStressDurationMinutes);
        Assert.Equal(0, settings.StressWorkerCount);
    }
}
