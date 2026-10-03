using System.Text.Json;
using GameTouchCompanion.Core;

namespace GameTouchCompanion.Core.Tests;

public sealed class JsonBrowserSettingsStoreTests
{
    [Fact]
    public async Task SaveAndLoadRoundTripBrowserPreferencesAndFavorites()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var settingsPath = Path.Combine(temporaryDirectory.Path, "nested", "browser.json");
        var store = new JsonBrowserSettingsStore(settingsPath);
        var expected = new BrowserSettings
        {
            HomeUrl = "https://example.com/wiki",
            ShowToolbar = false,
            OpenNewWindowsInTabs = false,
            ActivateNewWindowTabs = false,
            DefaultZoomPercent = 120,
            RememberZoomPerSite = true,
            SiteZoomPercentages = new Dictionary<string, int>
            {
                ["Example.COM"] = 90,
                ["fextralife.com"] = 130,
            },
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
        Assert.Equal(expected.OpenNewWindowsInTabs, actual.OpenNewWindowsInTabs);
        Assert.Equal(expected.ActivateNewWindowTabs, actual.ActivateNewWindowTabs);
        Assert.Equal(120, actual.DefaultZoomPercent);
        Assert.True(actual.RememberZoomPerSite);
        Assert.Equal(90, actual.SiteZoomPercentages["example.com"]);
        Assert.Equal(130, actual.SiteZoomPercentages["fextralife.com"]);
        Assert.Equal(expected.Favorites, actual.Favorites);
        Assert.Equal(Path.GetFullPath(settingsPath), store.FilePath);
        var json = await File.ReadAllTextAsync(settingsPath);
        Assert.Contains("\"showToolbar\": false", json, StringComparison.Ordinal);
        Assert.Contains("\"openNewWindowsInTabs\": false", json, StringComparison.Ordinal);
        Assert.Contains("\"activateNewWindowTabs\": false", json, StringComparison.Ordinal);
        Assert.Contains("\"defaultZoomPercent\": 120", json, StringComparison.Ordinal);
        Assert.Contains("\"rememberZoomPerSite\": true", json, StringComparison.Ordinal);
        Assert.Contains("\"example.com\": 90", json, StringComparison.Ordinal);
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
        Assert.True(settings.OpenNewWindowsInTabs);
        Assert.True(settings.ActivateNewWindowTabs);
        Assert.Equal(100, settings.DefaultZoomPercent);
        Assert.False(settings.RememberZoomPerSite);
        Assert.Empty(settings.SiteZoomPercentages);
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
        Assert.True(settings.OpenNewWindowsInTabs);
        Assert.True(settings.ActivateNewWindowTabs);
        Assert.Equal(100, settings.DefaultZoomPercent);
        Assert.False(settings.RememberZoomPerSite);
        Assert.Empty(settings.SiteZoomPercentages);
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

    [Fact]
    public async Task UserFriendlyAddressesAreCanonicalizedWhenSaved()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var store = new JsonBrowserSettingsStore(Path.Combine(temporaryDirectory.Path, "browser.json"));

        await store.SaveAsync(new BrowserSettings
        {
            HomeUrl = "google.com/start",
            Favorites = [new("Local", "localhost:8080/wiki")],
        });

        var saved = await store.LoadAsync();

        Assert.Equal("https://google.com/start", saved.HomeUrl);
        Assert.Equal(new BrowserFavorite("Local", "http://localhost:8080/wiki"), Assert.Single(saved.Favorites));
    }

    [Fact]
    public async Task SavingNormalizesRememberedZoomHosts()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var store = new JsonBrowserSettingsStore(Path.Combine(temporaryDirectory.Path, "browser.json"));

        await store.SaveAsync(new BrowserSettings
        {
            DefaultZoomPercent = 110,
            RememberZoomPerSite = true,
            SiteZoomPercentages = new Dictionary<string, int>
            {
                ["  EXAMPLE.COM.  "] = 90,
                ["LOCALHOST"] = 120,
            },
        });

        var saved = await store.LoadAsync();

        Assert.Equal(90, saved.SiteZoomPercentages["example.com"]);
        Assert.Equal(120, saved.SiteZoomPercentages["localhost"]);
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
    [InlineData("{\"favorites\":[{\"title\":\"\",\"url\":\"https://example.com/guide\"}]}")]
    [InlineData("{\"favorites\":[{\"title\":null,\"url\":\"https://example.com/guide\"}]}")]
    public async Task BlankFavoriteTitleIsAllowedAndFallsBackToUrl(string json)
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var settingsPath = Path.Combine(temporaryDirectory.Path, "browser.json");
        await File.WriteAllTextAsync(settingsPath, json);

        var settings = await new JsonBrowserSettingsStore(settingsPath).LoadAsync();

        var favorite = Assert.Single(settings.Favorites);
        Assert.Equal(string.Empty, favorite.Title);
        Assert.Equal("https://example.com/guide", favorite.Url);
        Assert.Equal(favorite.Url, favorite.DisplayText);
    }

    [Theory]
    [InlineData("null")]
    [InlineData("{\"homeUrl\":null}")]
    [InlineData("{\"homeUrl\":\"javascript:alert(1)\"}")]
    [InlineData("{\"homeUrl\":\"https://user:password@example.com/\"}")]
    [InlineData("{\"homeUrl\":\"file:///C:/private.json\"}")]
    [InlineData("{\"homeUrl\":\"https://touch-test.local/private.json\"}")]
    [InlineData("{\"defaultZoomPercent\":40}")]
    [InlineData("{\"defaultZoomPercent\":115}")]
    [InlineData("{\"defaultZoomPercent\":210}")]
    [InlineData("{\"siteZoomPercentages\":null}")]
    [InlineData("{\"siteZoomPercentages\":{\"bad host!\":100}}")]
    [InlineData("{\"siteZoomPercentages\":{\"example.com\":95}}")]
    [InlineData("{\"favorites\":null}")]
    [InlineData("{\"favorites\":[null]}")]
    [InlineData("{\"favorites\":[{\"title\":\"Example\",\"url\":null}]}")]
    [InlineData("{\"favorites\":[{\"title\":\"Example\",\"url\":\"javascript:alert(1)\"}]}")]
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
