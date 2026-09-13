using System.Text.Json;
using GameTouchCompanion.Core;

namespace GameTouchCompanion.Core.Tests;

public sealed class JsonBrowserSettingsStoreTests
{
    [Fact]
    public async Task SaveAndLoadRoundTripHomeToolbarAndFavorites()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var settingsPath = Path.Combine(temporaryDirectory.Path, "nested", "browser.json");
        var store = new JsonBrowserSettingsStore(settingsPath);
        var expected = new BrowserSettings
        {
            HomeUrl = "https://example.com/wiki",
            ShowToolbar = false,
            Favorites =
            [
                new("Game wiki", "https://example.com/wiki"),
                new("Touch test", BrowserUrlPolicy.LocalHomeUrl),
            ],
        };

        await store.SaveAsync(expected);
        var actual = await store.LoadAsync();

        Assert.Equal(expected.HomeUrl, actual.HomeUrl);
        Assert.Equal(expected.ShowToolbar, actual.ShowToolbar);
        Assert.Equal(expected.Favorites, actual.Favorites);
        Assert.Equal(Path.GetFullPath(settingsPath), store.FilePath);
        Assert.Contains("\"showToolbar\": false", await File.ReadAllTextAsync(settingsPath), StringComparison.Ordinal);
        Assert.Empty(Directory.EnumerateFiles(Path.GetDirectoryName(settingsPath)!, "*.tmp"));
    }

    [Fact]
    public async Task MissingFileLoadsSafeDefaultsWithoutCreatingAFile()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var settingsPath = Path.Combine(temporaryDirectory.Path, "missing", "browser.json");

        var settings = await new JsonBrowserSettingsStore(settingsPath).LoadAsync();

        Assert.Equal(BrowserUrlPolicy.LocalHomeUrl, settings.HomeUrl);
        Assert.True(settings.ShowToolbar);
        Assert.Empty(settings.Favorites);
        Assert.False(File.Exists(settingsPath));
        Assert.False(Directory.Exists(Path.GetDirectoryName(settingsPath)));
    }

    [Fact]
    public async Task MissingOptionalPropertiesUseDefaults()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var settingsPath = Path.Combine(temporaryDirectory.Path, "browser.json");
        await File.WriteAllTextAsync(settingsPath, "{}");

        var settings = await new JsonBrowserSettingsStore(settingsPath).LoadAsync();

        Assert.Equal(BrowserUrlPolicy.LocalHomeUrl, settings.HomeUrl);
        Assert.True(settings.ShowToolbar);
        Assert.Empty(settings.Favorites);
        Assert.Equal("{}", await File.ReadAllTextAsync(settingsPath));
    }

    [Fact]
    public async Task ExistingBrowserSettingsAreReplacedWithoutChangingMonitorSettings()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var settingsPath = Path.Combine(temporaryDirectory.Path, "browser.json");
        var monitorPath = Path.Combine(temporaryDirectory.Path, "settings.json");
        const string monitorContents = "{\"gameMonitorDeviceName\":\"DISPLAY1\"}";
        await File.WriteAllTextAsync(monitorPath, monitorContents);
        var store = new JsonBrowserSettingsStore(settingsPath);
        await store.SaveAsync(new BrowserSettings());

        await store.SaveAsync(new BrowserSettings { HomeUrl = "https://example.com/", ShowToolbar = false });
        var actual = await store.LoadAsync();

        Assert.Equal("https://example.com/", actual.HomeUrl);
        Assert.False(actual.ShowToolbar);
        Assert.Equal(monitorContents, await File.ReadAllTextAsync(monitorPath));
        Assert.Empty(Directory.EnumerateFiles(temporaryDirectory.Path, "*.tmp"));
    }

    [Fact]
    public async Task SavingNormalizesUrlsAndTitlesWithoutMutatingTheCaller()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var store = new JsonBrowserSettingsStore(Path.Combine(temporaryDirectory.Path, "browser.json"));
        var original = new BrowserSettings
        {
            HomeUrl = "  HTTPS://EXAMPLE.COM:443  ",
            Favorites = [new("  Example  ", "https://EXAMPLE.COM")],
        };

        await store.SaveAsync(original);
        var saved = await store.LoadAsync();

        Assert.Equal("https://example.com/", saved.HomeUrl);
        Assert.Equal(new BrowserFavorite("Example", "https://example.com/"), Assert.Single(saved.Favorites));
        Assert.Equal("  HTTPS://EXAMPLE.COM:443  ", original.HomeUrl);
        Assert.Equal("  Example  ", original.Favorites[0].Title);
    }

    [Theory]
    [InlineData("{ invalid json }")]
    [InlineData("[]")]
    [InlineData("{\"showToolbar\":\"yes\"}")]
    public async Task InvalidJsonIsReportedAndPreserved(string original)
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var settingsPath = Path.Combine(temporaryDirectory.Path, "browser.json");
        await File.WriteAllTextAsync(settingsPath, original);

        await Assert.ThrowsAsync<JsonException>(() => new JsonBrowserSettingsStore(settingsPath).LoadAsync());

        Assert.Equal(original, await File.ReadAllTextAsync(settingsPath));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{\"homeUrl\":null}")]
    [InlineData("{\"homeUrl\":\"javascript:alert(1)\"}")]
    [InlineData("{\"homeUrl\":\"https://user:password@example.com/\"}")]
    [InlineData("{\"homeUrl\":\"file:///C:/private.json\"}")]
    [InlineData("{\"homeUrl\":\"https://touch-test.local/private.json\"}")]
    [InlineData("{\"favorites\":null}")]
    [InlineData("{\"favorites\":[null]}")]
    [InlineData("{\"favorites\":[{\"title\":\"\",\"url\":\"https://example.com/\"}]}")]
    [InlineData("{\"favorites\":[{\"title\":null,\"url\":\"https://example.com/\"}]}")]
    [InlineData("{\"favorites\":[{\"title\":\"Example\",\"url\":null}]}")]
    [InlineData("{\"favorites\":[{\"title\":\"Example\",\"url\":\"example.com\"}]}")]
    [InlineData("{\"favorites\":[{\"title\":\"Example\",\"url\":\"file:///C:/private.txt\"}]}")]
    public async Task InvalidSettingsDataIsReportedAndPreserved(string original)
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var settingsPath = Path.Combine(temporaryDirectory.Path, "browser.json");
        await File.WriteAllTextAsync(settingsPath, original);

        await Assert.ThrowsAsync<InvalidDataException>(() => new JsonBrowserSettingsStore(settingsPath).LoadAsync());

        Assert.Equal(original, await File.ReadAllTextAsync(settingsPath));
    }

    [Fact]
    public async Task InvalidSavePreservesExistingSettings()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var settingsPath = Path.Combine(temporaryDirectory.Path, "browser.json");
        var store = new JsonBrowserSettingsStore(settingsPath);
        await store.SaveAsync(new BrowserSettings());
        var original = await File.ReadAllTextAsync(settingsPath);

        await Assert.ThrowsAsync<InvalidDataException>(() => store.SaveAsync(new BrowserSettings
        {
            Favorites = [new("Unsafe", "javascript:alert(1)")],
        }));

        Assert.Equal(original, await File.ReadAllTextAsync(settingsPath));
        Assert.Empty(Directory.EnumerateFiles(temporaryDirectory.Path, "*.tmp"));
    }

    [Fact]
    public async Task CancelledSaveDoesNotCreateSettingsOrTemporaryFiles()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var settingsPath = Path.Combine(temporaryDirectory.Path, "browser.json");
        var store = new JsonBrowserSettingsStore(settingsPath);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.SaveAsync(new BrowserSettings(), cancellation.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.LoadAsync(cancellation.Token));

        Assert.Empty(Directory.EnumerateFiles(temporaryDirectory.Path));
    }

    [Fact]
    public void DefaultPathIsSeparateFromMonitorSettings()
    {
        Assert.Equal(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GameTouchCompanion", "browser.json"),
            JsonBrowserSettingsStore.GetDefaultFilePath());
        Assert.NotEqual(JsonApplicationSettingsStore.GetDefaultFilePath(), JsonBrowserSettingsStore.GetDefaultFilePath());
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"GameTouchCompanion.Browser.Tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
