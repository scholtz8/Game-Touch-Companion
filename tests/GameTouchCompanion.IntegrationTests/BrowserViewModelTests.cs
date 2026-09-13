using System.IO;
using System.Text.Json;
using GameTouchCompanion.App;
using GameTouchCompanion.Core;

namespace GameTouchCompanion.IntegrationTests;

public sealed class BrowserViewModelTests
{
    [Fact]
    public async Task InitializeLoadsPreferencesWithoutSavingOrRequestingNavigation()
    {
        var stored = new BrowserSettings
        {
            HomeUrl = "https://example.com/wiki",
            ShowToolbar = false,
            Favorites = [new("Wiki", "https://example.com/wiki")],
        };
        var store = new MemoryBrowserSettingsStore(stored);
        var viewModel = new BrowserViewModel(store);
        var navigations = new List<string>();
        viewModel.NavigationRequested += navigations.Add;

        await viewModel.InitializeAsync();

        Assert.True(viewModel.SettingsLoaded);
        Assert.True(viewModel.CanEditSettings);
        Assert.False(viewModel.HasError);
        Assert.Equal(stored.HomeUrl, viewModel.HomeUrl);
        Assert.Equal(stored.HomeUrl, viewModel.Address);
        Assert.False(viewModel.ShowToolbar);
        Assert.Equal(stored.Favorites, viewModel.Favorites);
        Assert.False(viewModel.IsReady);
        Assert.Empty(viewModel.CurrentUrl);
        Assert.Null(viewModel.RequestedUrl);
        Assert.Empty(navigations);
        Assert.Equal(0, store.SaveAttempts);
    }

    [Fact]
    public async Task FailedLoadBlocksPreferenceWritesAndPreservesOriginalSettings()
    {
        var original = new BrowserSettings
        {
            HomeUrl = "https://example.com/original",
            Favorites = [new("Original", "https://example.com/original")],
        };
        var store = new MemoryBrowserSettingsStore(original)
        {
            LoadFailure = new JsonException("The file is corrupt."),
        };
        var viewModel = new BrowserViewModel(store);

        await viewModel.InitializeAsync();

        Assert.False(viewModel.SettingsLoaded);
        Assert.False(viewModel.CanEditSettings);
        Assert.True(viewModel.HasError);
        Assert.Contains("browser.json", viewModel.Error, StringComparison.Ordinal);

        Assert.True(viewModel.GoHome());
        viewModel.ReportReady();
        Assert.True(viewModel.HasSettingsError);
        Assert.Contains("browser.json", viewModel.SettingsError, StringComparison.Ordinal);
        Assert.False(viewModel.CanEditSettings);

        viewModel.Address = "https://example.com/replacement";
        await viewModel.SetHomeFromAddressAsync();
        await viewModel.AddFavoriteAsync("https://example.com/new");
        await viewModel.RemoveFavoriteAsync(original.Favorites[0]);
        await viewModel.SetToolbarVisibleAsync(false);

        Assert.Equal(0, store.SaveAttempts);
        Assert.Same(original, store.Current);
        Assert.Equal(BrowserUrlPolicy.LocalHomeUrl, viewModel.HomeUrl);
        Assert.Empty(viewModel.Favorites);
        Assert.False(viewModel.ShowToolbar);
        Assert.True(viewModel.HasError);
    }

    [Fact]
    public void NavigationBeforeReadyQueuesNormalizedUrlAndRejectsInvalidReplacement()
    {
        var store = new MemoryBrowserSettingsStore(new BrowserSettings());
        var viewModel = new BrowserViewModel(store);
        var navigations = new List<string>();
        viewModel.NavigationRequested += navigations.Add;
        viewModel.Address = "  HTTPS://EXAMPLE.COM:443/wiki  ";

        Assert.True(viewModel.NavigateAddress());

        Assert.False(viewModel.IsReady);
        Assert.Equal("https://example.com/wiki", viewModel.Address);
        Assert.Equal("https://example.com/wiki", viewModel.RequestedUrl);
        Assert.Equal("https://example.com/wiki", Assert.Single(navigations));
        Assert.Empty(viewModel.CurrentUrl);

        viewModel.Address = "javascript:alert(1)";
        Assert.False(viewModel.NavigateAddress());

        Assert.True(viewModel.HasError);
        Assert.Equal("https://example.com/wiki", viewModel.RequestedUrl);
        Assert.Single(navigations);
        Assert.Empty(viewModel.CurrentUrl);
        Assert.Equal(0, store.SaveAttempts);
    }

    [Fact]
    public void BrowserLifecycleOnlyEnablesHistoryWhileReadyAndPreservesEditedAddress()
    {
        var viewModel = new BrowserViewModel(new MemoryBrowserSettingsStore(new BrowserSettings()));
        viewModel.ReportHistory(true, true);
        Assert.False(viewModel.CanGoBack);
        Assert.False(viewModel.CanGoForward);

        viewModel.ReportReady();
        viewModel.ReportHistory(true, true);
        viewModel.Address = "https://example.com/address-being-edited";
        viewModel.ReportNavigation("https://example.com/loaded");

        Assert.True(viewModel.IsReady);
        Assert.True(viewModel.CanGoBack);
        Assert.True(viewModel.CanGoForward);
        Assert.Equal("https://example.com/loaded", viewModel.CurrentUrl);
        Assert.Equal("https://example.com/loaded", viewModel.RequestedUrl);
        Assert.Equal("https://example.com/address-being-edited", viewModel.Address);

        viewModel.ReportNavigation("file:///C:/private.json");
        Assert.Equal("https://example.com/loaded", viewModel.CurrentUrl);

        viewModel.ReportClosed();
        Assert.False(viewModel.IsReady);
        Assert.False(viewModel.CanGoBack);
        Assert.False(viewModel.CanGoForward);
        Assert.Equal("https://example.com/loaded", viewModel.RequestedUrl);

        viewModel.ReportHistory(true, true);
        Assert.False(viewModel.CanGoBack);
        Assert.False(viewModel.CanGoForward);
    }

    [Fact]
    public async Task FavoritesCanonicalizeDeduplicateAndRemoveSelectedItem()
    {
        var store = new MemoryBrowserSettingsStore(new BrowserSettings());
        var viewModel = new BrowserViewModel(store);
        await viewModel.InitializeAsync();

        await viewModel.AddFavoriteAsync("  HTTPS://EXAMPLE.COM:443  ", "  Example wiki  ");
        var favorite = Assert.Single(viewModel.Favorites);
        Assert.Equal(new BrowserFavorite("Example wiki", "https://example.com/"), favorite);
        Assert.Equal(favorite, Assert.Single(store.Current.Favorites));

        await viewModel.AddFavoriteAsync("https://example.com/", "Duplicate title");
        Assert.Single(viewModel.Favorites);
        Assert.Equal(1, store.SaveAttempts);

        viewModel.SelectedFavorite = favorite;
        await viewModel.RemoveFavoriteAsync(favorite);
        Assert.Empty(viewModel.Favorites);
        Assert.Empty(store.Current.Favorites);
        Assert.Null(viewModel.SelectedFavorite);
        Assert.Equal(2, store.SaveAttempts);

        await viewModel.RemoveFavoriteAsync(favorite);
        Assert.Equal(2, store.SaveAttempts);
    }

    [Fact]
    public async Task AddFavoriteUsesLoadedPageEvenWhileAnotherAddressIsBeingEdited()
    {
        var store = new MemoryBrowserSettingsStore(new BrowserSettings());
        var viewModel = new BrowserViewModel(store);
        await viewModel.InitializeAsync();
        viewModel.ReportNavigation("https://example.com/current");
        viewModel.Address = "an incomplete address";

        await viewModel.AddFavoriteAsync();

        Assert.Equal(new BrowserFavorite("example.com", "https://example.com/current"), Assert.Single(viewModel.Favorites));
        Assert.False(viewModel.HasError);
    }

    [Theory]
    [InlineData("file:///C:/private.json")]
    [InlineData("https://user:password@example.com/")]
    [InlineData("https://touch-test.local/private.json")]
    [InlineData("example.com")]
    public async Task InvalidHomeAndFavoriteDoNotPersistOrRequestNavigation(string input)
    {
        var store = new MemoryBrowserSettingsStore(new BrowserSettings());
        var viewModel = new BrowserViewModel(store);
        var navigations = new List<string>();
        viewModel.NavigationRequested += navigations.Add;
        await viewModel.InitializeAsync();
        viewModel.Address = input;

        await viewModel.SetHomeFromAddressAsync();
        await viewModel.AddFavoriteAsync(input);

        Assert.Equal(BrowserUrlPolicy.LocalHomeUrl, viewModel.HomeUrl);
        Assert.Empty(viewModel.Favorites);
        Assert.Empty(navigations);
        Assert.Equal(0, store.SaveAttempts);
        Assert.True(viewModel.HasError);
    }

    [Fact]
    public async Task HomeAndToolbarPersistAndHomeNavigationUsesSavedUrl()
    {
        var store = new MemoryBrowserSettingsStore(new BrowserSettings());
        var viewModel = new BrowserViewModel(store);
        await viewModel.InitializeAsync();
        var navigations = new List<string>();
        viewModel.NavigationRequested += navigations.Add;
        viewModel.Address = "  HTTPS://EXAMPLE.COM:443/start  ";

        await viewModel.SetHomeFromAddressAsync();
        await viewModel.SetToolbarVisibleAsync(false);

        Assert.Equal("https://example.com/start", store.Current.HomeUrl);
        Assert.Equal(store.Current.HomeUrl, viewModel.HomeUrl);
        Assert.Equal(store.Current.HomeUrl, viewModel.Address);
        Assert.False(store.Current.ShowToolbar);
        Assert.False(viewModel.ShowToolbar);
        Assert.Equal(2, store.SaveAttempts);
        Assert.Empty(navigations);

        viewModel.Address = "https://example.com/elsewhere";
        Assert.True(viewModel.GoHome());
        Assert.Equal("https://example.com/start", Assert.Single(navigations));

        await viewModel.SetToolbarVisibleAsync(true);
        Assert.True(viewModel.ShowToolbar);
        Assert.True(store.Current.ShowToolbar);
    }

    [Fact]
    public async Task FailedSaveReportsInMemoryStateAndLaterSaveCanRecover()
    {
        var store = new MemoryBrowserSettingsStore(new BrowserSettings())
        {
            SaveFailure = new IOException("The file is unavailable."),
        };
        var viewModel = new BrowserViewModel(store);
        await viewModel.InitializeAsync();
        viewModel.Address = "https://example.com/unsaved";

        await viewModel.SetHomeFromAddressAsync();

        Assert.True(viewModel.HasError);
        Assert.Contains("memoria", viewModel.Error, StringComparison.Ordinal);
        Assert.Contains("browser.json", viewModel.Error, StringComparison.Ordinal);
        Assert.Equal("https://example.com/unsaved", viewModel.HomeUrl);
        Assert.Equal(BrowserUrlPolicy.LocalHomeUrl, store.Current.HomeUrl);
        Assert.True(viewModel.CanEditSettings);

        viewModel.ReportStatus("Página lista.");
        Assert.True(viewModel.HasSettingsError);
        Assert.Contains("memoria", viewModel.SettingsError, StringComparison.Ordinal);

        store.SaveFailure = null;
        await viewModel.SetToolbarVisibleAsync(false);

        Assert.False(viewModel.HasError);
        Assert.False(viewModel.HasSettingsError);
        Assert.Equal("https://example.com/unsaved", store.Current.HomeUrl);
        Assert.False(store.Current.ShowToolbar);
    }

    [Fact]
    public async Task OverlappingChangesSerializeSavesAndFinalSnapshotContainsEveryChange()
    {
        var store = new ControlledBrowserSettingsStore();
        var viewModel = new BrowserViewModel(store);
        await viewModel.InitializeAsync();
        viewModel.Address = "https://example.com/home";

        var homeSave = viewModel.SetHomeFromAddressAsync();
        await store.FirstSaveStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var toolbarSave = viewModel.SetToolbarVisibleAsync(false);
        var favoriteSave = viewModel.AddFavoriteAsync("https://example.com/wiki", "Wiki");

        Assert.Equal(1, store.SaveAttempts);
        Assert.False(homeSave.IsCompleted);
        Assert.False(toolbarSave.IsCompleted);
        Assert.False(favoriteSave.IsCompleted);
        Assert.Equal("https://example.com/home", store.Snapshots[0].HomeUrl);
        Assert.True(store.Snapshots[0].ShowToolbar);
        Assert.Empty(store.Snapshots[0].Favorites);

        store.ReleaseFirstSave.TrySetResult();
        await Task.WhenAll(homeSave, toolbarSave, favoriteSave).WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(3, store.SaveAttempts);
        Assert.Equal(1, store.MaximumConcurrentSaves);
        var final = store.Snapshots[^1];
        Assert.Equal("https://example.com/home", final.HomeUrl);
        Assert.False(final.ShowToolbar);
        Assert.Equal(new BrowserFavorite("Wiki", "https://example.com/wiki"), Assert.Single(final.Favorites));
        Assert.False(viewModel.HasError);
    }

    private sealed class MemoryBrowserSettingsStore(BrowserSettings initial) : IBrowserSettingsStore
    {
        public string FilePath => "memory://browser.json";
        public BrowserSettings Current { get; private set; } = initial;
        public Exception? LoadFailure { get; init; }
        public Exception? SaveFailure { get; set; }
        public int SaveAttempts { get; private set; }

        public Task<BrowserSettings> LoadAsync(CancellationToken cancellationToken = default) =>
            LoadFailure is null
                ? Task.FromResult(Current with { Favorites = [.. Current.Favorites] })
                : Task.FromException<BrowserSettings>(LoadFailure);

        public Task SaveAsync(BrowserSettings settings, CancellationToken cancellationToken = default)
        {
            SaveAttempts++;
            if (SaveFailure is not null) return Task.FromException(SaveFailure);
            Current = settings with { Favorites = [.. settings.Favorites] };
            return Task.CompletedTask;
        }
    }

    private sealed class ControlledBrowserSettingsStore : IBrowserSettingsStore
    {
        private int activeSaves;
        public string FilePath => "memory://browser.json";
        public TaskCompletionSource FirstSaveStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ReleaseFirstSave { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public List<BrowserSettings> Snapshots { get; } = [];
        public int SaveAttempts { get; private set; }
        public int MaximumConcurrentSaves { get; private set; }

        public Task<BrowserSettings> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(new BrowserSettings());

        public async Task SaveAsync(BrowserSettings settings, CancellationToken cancellationToken = default)
        {
            var concurrentSaves = Interlocked.Increment(ref activeSaves);
            MaximumConcurrentSaves = Math.Max(MaximumConcurrentSaves, concurrentSaves);
            SaveAttempts++;
            Snapshots.Add(settings with { Favorites = [.. settings.Favorites] });
            try
            {
                if (SaveAttempts == 1)
                {
                    FirstSaveStarted.TrySetResult();
                    await ReleaseFirstSave.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
                }
            }
            finally
            {
                Interlocked.Decrement(ref activeSaves);
            }
        }
    }
}
