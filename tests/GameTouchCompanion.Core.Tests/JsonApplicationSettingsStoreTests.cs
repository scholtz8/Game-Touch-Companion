using System.Text.Json;
using GameTouchCompanion.Core;

namespace GameTouchCompanion.Core.Tests;

public sealed class JsonApplicationSettingsStoreTests
{
    [Fact]
    public async Task SaveAndLoadRoundTripAllMonitorSettings()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var settingsPath = Path.Combine(temporaryDirectory.Path, "nested", "settings.json");
        var store = new JsonApplicationSettingsStore(settingsPath);
        var expected = new ApplicationSettings
        {
            GameMonitorDeviceName = "\\\\.\\DISPLAY1",
            CompanionMonitorDeviceName = "\\\\.\\DISPLAY2",
            AllowSameMonitorForTesting = true,
            EnableDetectionOnStartup = true,
            StartMinimizedToTray = true,
            CloseToTray = true,
        };

        await store.SaveAsync(expected);
        var actual = await store.LoadAsync();
        var json = await File.ReadAllTextAsync(settingsPath);

        Assert.Equal(expected, actual);
        Assert.Equal(Path.GetFullPath(settingsPath), store.FilePath);
        Assert.Contains("\"gameMonitorDeviceName\"", json, StringComparison.Ordinal);
        Assert.Contains("\"allowSameMonitorForTesting\": true", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MissingFileLoadsDefaultSettingsWithoutCreatingAFile()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var settingsPath = Path.Combine(temporaryDirectory.Path, "missing", "settings.json");
        var store = new JsonApplicationSettingsStore(settingsPath);

        var settings = await store.LoadAsync();

        Assert.Equal(new ApplicationSettings(), settings);
        Assert.False(File.Exists(settingsPath));
    }

    [Fact]
    public async Task LegacySettingsDefaultStartupDetectionToFalse()
    {
        using var directory = new TemporaryDirectory();
        var path = Path.Combine(directory.Path, "settings.json");
        await File.WriteAllTextAsync(path, "{\"allowSameMonitorForTesting\":false}");
        Assert.False((await new JsonApplicationSettingsStore(path).LoadAsync()).EnableDetectionOnStartup);
    }

    [Fact]
    public async Task ExistingSettingsCanBeOverwrittenAtomically()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var settingsPath = Path.Combine(temporaryDirectory.Path, "settings.json");
        var store = new JsonApplicationSettingsStore(settingsPath);
        await store.SaveAsync(new ApplicationSettings
        {
            GameMonitorDeviceName = "\\\\.\\DISPLAY1",
        });

        var expected = new ApplicationSettings
        {
            GameMonitorDeviceName = "\\\\.\\DISPLAY2",
            CompanionMonitorDeviceName = "\\\\.\\DISPLAY3",
        };
        await store.SaveAsync(expected);

        Assert.Equal(expected, await store.LoadAsync());
        Assert.Empty(Directory.EnumerateFiles(temporaryDirectory.Path, "*.tmp"));
    }

    [Fact]
    public async Task InvalidJsonIsReportedToTheCaller()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var settingsPath = Path.Combine(temporaryDirectory.Path, "settings.json");
        await File.WriteAllTextAsync(settingsPath, "{ invalid json }");
        var store = new JsonApplicationSettingsStore(settingsPath);

        await Assert.ThrowsAsync<JsonException>(() => store.LoadAsync());
    }

    [Fact]
    public void DefaultPathUsesTheLocalApplicationDataDirectory()
    {
        var localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        var path = JsonApplicationSettingsStore.GetDefaultFilePath();

        Assert.Equal(
            Path.Combine(localApplicationData, "GameTouchCompanion", "settings.json"),
            path);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"GameTouchCompanion.Core.Tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
