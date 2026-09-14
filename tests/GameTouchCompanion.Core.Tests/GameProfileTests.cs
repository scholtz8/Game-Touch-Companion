using System.Text.Json;
using GameTouchCompanion.Core;

namespace GameTouchCompanion.Core.Tests;

public sealed class GameProfileTests
{
    private static GameProfile Valid() => new() { Id = "test-game", DisplayName = "Juego", ProcessName = "Game.exe" };

    [Fact]
    public void NormalizesFieldsAndPreservesSafetyAndFutureAutoLaunch()
    {
        var profile = GameProfileValidation.Normalize(Valid() with
        {
            DisplayName = " Juego ", ProcessName = " Game.exe ", Url = " HTTPS://EXAMPLE.COM:443/map ",
            CompanionMonitor = " DISPLAY2 ", AutoLaunch = true,
        });
        Assert.Equal("Juego", profile.DisplayName);
        Assert.Equal("Game.exe", profile.ProcessName);
        Assert.Equal("https://example.com/map", profile.Url);
        Assert.Single(profile.Tabs);
        Assert.Equal(profile.Tabs[0].Id, profile.PrimaryTabId);
        Assert.Equal("https://example.com/map", profile.PrimaryTab.Url);
        Assert.Equal("DISPLAY2", profile.CompanionMonitor);
        Assert.True(profile.AutoLaunch);
        Assert.True(profile.NoActivate);
        Assert.False(profile.RestoreGameFocusFallback);
        Assert.Null(GameProfileValidation.Normalize(profile with { CompanionMonitor = " " }).CompanionMonitor);
    }

    [Theory]
    [InlineData("C:\\Games\\Game.exe")]
    [InlineData("Game.exe --flag")]
    [InlineData("Game")]
    [InlineData(".exe")]
    [InlineData("../Game.exe")]
    [InlineData("Game\n.exe")]
    public void RejectsProcessPathsArgumentsAndMalformedNames(string process) =>
        Assert.Throws<InvalidDataException>(() => GameProfileValidation.Normalize(Valid() with { ProcessName = process }));

    [Fact]
    public void RejectsInvalidNameUrlIdentityAndUnsafeFocusOptions()
    {
        GameProfile[] invalid =
        [
            Valid() with { Id = "../escape" }, Valid() with { DisplayName = " " },
            Valid() with { Url = "file:///C:/private.txt" }, Valid() with { Url = "https://user:secret@example.com" },
            Valid() with { NoActivate = false }, Valid() with { RestoreGameFocusFallback = true },
            Valid() with { ProcessName = "", AutoLaunch = true }, Valid() with { CompanionMonitor = "bad\nmonitor" },
        ];
        foreach (var profile in invalid) Assert.Throws<InvalidDataException>(() => GameProfileValidation.Normalize(profile));
        Assert.Empty(GameProfileValidation.Normalize(Valid() with { ProcessName = "" }).ProcessName);
    }

    [Fact]
    public void RejectsFutureSchemaNullEntriesAndDuplicateIds()
    {
        Assert.Throws<InvalidDataException>(() => GameProfileValidation.Normalize(new GameProfileDocument { SchemaVersion = 3 }));
        Assert.Throws<InvalidDataException>(() => GameProfileValidation.Normalize(new GameProfileDocument { Profiles = [null!] }));
        Assert.Throws<InvalidDataException>(() => GameProfileValidation.Normalize(new GameProfileDocument { Profiles = [Valid(), Valid() with { Id = "TEST-GAME" }] }));
    }

    [Fact]
    public async Task StoreRoundTripReplaceAndCancellationPreserveIndependentFiles()
    {
        var folder = Path.Combine(Path.GetTempPath(), "GameTouchCompanion.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var store = new JsonGameProfileStore(Path.Combine(folder, "profiles.json"));
            Assert.Empty((await store.LoadAsync()).Profiles);
            Assert.False(File.Exists(store.FilePath));
            var browser = new JsonBrowserSettingsStore(Path.Combine(folder, "browser.json"));
            await browser.SaveAsync(new BrowserSettings());
            var previousBrowser = await File.ReadAllTextAsync(browser.FilePath);
            await store.SaveAsync(new GameProfileDocument { Profiles = [Valid() with { AutoLaunch = true }] });
            var loaded = await store.LoadAsync();
            var loadedProfile = Assert.Single(loaded.Profiles);
            Assert.True(loadedProfile.AutoLaunch);
            Assert.Equal("test-game", loadedProfile.Id);
            Assert.Single(loadedProfile.Tabs);
            Assert.Equal(loadedProfile.PrimaryTab.Id, loadedProfile.PrimaryTabId);
            var before = await File.ReadAllTextAsync(store.FilePath);
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => store.SaveAsync(new GameProfileDocument(), new CancellationToken(true)));
            await Assert.ThrowsAsync<InvalidDataException>(() => store.SaveAsync(new GameProfileDocument { SchemaVersion = 99 }));
            Assert.Equal(before, await File.ReadAllTextAsync(store.FilePath));
            await store.SaveAsync(new GameProfileDocument());
            Assert.Empty((await store.LoadAsync()).Profiles);
            Assert.Equal(previousBrowser, await File.ReadAllTextAsync(browser.FilePath));
            Assert.Empty(Directory.GetFiles(folder, "*.tmp"));
        }
        finally { Directory.Delete(folder, recursive: true); }
    }

    [Fact]
    public async Task SchemaOneProfilesAreMigratedToTabsAndPersistedAsSchemaTwo()
    {
        var folder = Path.Combine(Path.GetTempPath(), "GameTouchCompanion.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var path = Path.Combine(folder, "profiles.json");
            await File.WriteAllTextAsync(path, "{\"schemaVersion\":1,\"profiles\":[{\"id\":\"legacy\",\"displayName\":\"Legacy\",\"processName\":\"Game.exe\",\"url\":\"https://example.com/map\",\"noActivate\":true,\"restoreGameFocusFallback\":false}]}");
            var store = new JsonGameProfileStore(path);
            var document = await store.LoadAsync();
            Assert.Equal(2, document.SchemaVersion);
            var profile = Assert.Single(document.Profiles);
            Assert.Single(profile.Tabs);
            Assert.Equal("https://example.com/map", profile.PrimaryTab.Url);
            var persisted = await File.ReadAllTextAsync(path);
            Assert.Contains("\"schemaVersion\": 2", persisted);
            Assert.Contains("\"tabs\"", persisted);
        }
        finally { Directory.Delete(folder, recursive: true); }
    }

    [Theory]
    [InlineData("broken")]
    [InlineData("null")]
    [InlineData("{\"schemaVersion\":99,\"profiles\":[]}")]
    [InlineData("{\"profiles\":null}")]
    [InlineData("{\"schemaVersion\":1,\"profiles\":[{\"displayName\":\"Missing ID\",\"url\":\"https://example.com/\"}]}")]
    public async Task BadDocumentsAreReportedWithoutOverwriting(string content)
    {
        var folder = Path.Combine(Path.GetTempPath(), "GameTouchCompanion.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var store = new JsonGameProfileStore(Path.Combine(folder, "profiles.json"));
            await File.WriteAllTextAsync(store.FilePath, content);
            var exception = await Record.ExceptionAsync(() => store.LoadAsync());
            Assert.True(exception is JsonException or InvalidDataException);
            Assert.Equal(content, await File.ReadAllTextAsync(store.FilePath));
        }
        finally { Directory.Delete(folder, recursive: true); }
    }
}
