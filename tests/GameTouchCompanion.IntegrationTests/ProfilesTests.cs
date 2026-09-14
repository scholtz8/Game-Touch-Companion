using System.IO;
using GameTouchCompanion.App;
using GameTouchCompanion.Core;
using GameTouchCompanion.Native;

namespace GameTouchCompanion.IntegrationTests;

public sealed class ProfilesTests
{
    [Fact]
    public async Task InitializeCreatesCleanDefaultTabDraft()
    {
        var model = new ProfilesViewModel(new ProfileStore());
        await model.InitializeAsync();

        var tab = Assert.Single(model.Tabs);
        Assert.True(tab.IsPrimary);
        Assert.Equal(BrowserUrlPolicy.LocalHomeUrl, tab.Url);
        Assert.False(model.HasUnsavedChanges);
    }

    [Fact]
    public async Task StartupPreferenceSurvivesSelectionRefreshProfileAndReload()
    {
        var store = new SettingsStore();
        var model = new MainWindowViewModel(new Monitors(), store, new MonitorSelectionService());
        await Assert.ThrowsAsync<InvalidDataException>(() => model.SetDetectionOnStartupAsync(true));
        await model.InitializeAsync();
        Assert.False(model.EnableDetectionOnStartup);
        await model.SetDetectionOnStartupAsync(true);
        await model.SetTrayPreferencesAsync(true, true);
        await model.RefreshAsync();
        await model.ApplySelectionAsync();
        await model.ApplyProfileMonitorAsync("DISPLAY3");
        Assert.True(store.Settings.EnableDetectionOnStartup);
        var reloaded = new MainWindowViewModel(new Monitors(), store, new MonitorSelectionService());
        await reloaded.InitializeAsync();
        Assert.True(reloaded.EnableDetectionOnStartup);
        Assert.True(reloaded.StartMinimizedToTray);
        Assert.True(reloaded.CloseToTray);
        store.FailSave = true;
        await Assert.ThrowsAsync<IOException>(() => reloaded.SetTrayPreferencesAsync(false, false));
        Assert.True(reloaded.StartMinimizedToTray);
        Assert.True(reloaded.CloseToTray);
        await Assert.ThrowsAsync<IOException>(() => reloaded.SetDetectionOnStartupAsync(false));
        Assert.True(reloaded.EnableDetectionOnStartup);
        Assert.True(store.Settings.EnableDetectionOnStartup);
        store.FailSave = false;
        await reloaded.SetDetectionOnStartupAsync(false);
        Assert.False(store.Settings.EnableDetectionOnStartup);
    }

    [Fact]
    public async Task SwitchingWithDirtyDraftRequiresExplicitDiscardOrCancel()
    {
        var model = new ProfilesViewModel(new ProfileStore());
        await model.InitializeAsync();
        model.DisplayName = "Uno";
        await model.SaveAsync();
        var first = model.SelectedProfile;
        model.NewProfile(); model.DisplayName = "Dos";
        await model.SaveAsync();
        var second = model.SelectedProfile;
        model.Tabs[0].Url = "https://example.com/draft";
        Assert.True(model.HasUnsavedChanges);
        model.SelectedProfile = first;
        Assert.True(model.DiscardConfirmation);
        Assert.Equal(second, model.SelectedProfile);
        model.CancelDiscard();
        Assert.Equal("https://example.com/draft", model.Tabs[0].Url);
        model.SelectedProfile = first;
        model.ConfirmDiscard();
        Assert.Equal(first, model.SelectedProfile);
        Assert.False(model.HasUnsavedChanges);
        Assert.False(model.DiscardConfirmation);
    }

    [Fact]
    public async Task NewDraftAndFailedSaveKeepPendingChanges()
    {
        var store = new ProfileStore();
        var model = new ProfilesViewModel(store);
        await model.InitializeAsync();
        model.DisplayName = "Sin guardar";
        model.CompanionMonitor = "DISCONNECTED";
        model.NewProfile();
        Assert.True(model.DiscardConfirmation);
        model.CancelDiscard();
        Assert.Equal("DISCONNECTED", model.CompanionMonitor);
        store.FailSave = true;
        await model.SaveAsync();
        Assert.True(model.HasUnsavedChanges);
        Assert.Empty(model.Profiles);
        store.FailSave = false;
        await model.SaveAsync();
        Assert.False(model.HasUnsavedChanges);
        Assert.Equal("DISCONNECTED", model.SelectedProfile!.CompanionMonitor);
        model.DisplayName = "Otro";
        model.NewProfile(); model.ConfirmDiscard();
        Assert.Null(model.SelectedProfile);
        Assert.False(model.HasUnsavedChanges);
        Assert.Single(model.Profiles);
    }

    [Fact]
    public async Task CreateEditApplySavedVersionAndConfirmDelete()
    {
        var store = new ProfileStore();
        var model = new ProfilesViewModel(store);
        await model.InitializeAsync();
        model.NewProfile();
        model.DisplayName = "Uno";
        model.ProcessName = "Game.exe";
        model.AutoLaunch = true;
        await model.SaveAsync();
        var saved = Assert.Single(model.Profiles);
        Assert.True(saved.AutoLaunch);
        model.DisplayName = "Dos";
        await model.SaveAsync();
        Assert.Equal(saved.Id, Assert.Single(model.Profiles).Id);
        Assert.Equal("Dos", model.SelectedProfile!.DisplayName);
        model.Tabs[0].Url = "https://example.com/unsaved";
        GameProfile? applied = null;
        await model.ApplyAsync(p => { applied = p; return Task.CompletedTask; });
        Assert.Equal(BrowserUrlPolicy.LocalHomeUrl, applied!.Url);
        await model.DeleteAsync();
        Assert.Single(model.Profiles);
        model.RequestDelete();
        model.CancelDelete();
        await model.DeleteAsync();
        Assert.Single(model.Profiles);
        model.RequestDelete();
        await model.DeleteAsync();
        Assert.Empty(model.Profiles);
        Assert.Empty(store.Document.Profiles);
        Assert.Null(model.SelectedProfile);
    }

    [Fact]
    public async Task ProfileEditorPersistsMultipleTabsOrderAndPrimarySelection()
    {
        var store = new ProfileStore();
        var model = new ProfilesViewModel(store);
        await model.InitializeAsync();
        model.DisplayName = "Multi";
        model.ProcessName = "Game.exe";
        model.Tabs[0].Name = "Mapa";
        model.Tabs[0].Url = "https://example.com/map";
        model.AddTab();
        model.Tabs[1].Name = "Wiki";
        model.Tabs[1].Url = "https://example.com/wiki";
        model.SetPrimary(model.Tabs[1]);
        model.MoveTab(model.Tabs[1], -1);
        await model.SaveAsync();
        var saved = Assert.Single(store.Document.Profiles);
        Assert.Equal(2, saved.Tabs.Count);
        Assert.Equal("Wiki", saved.Tabs[0].Name);
        Assert.Equal("Wiki", saved.PrimaryTab.Name);
        Assert.Equal("https://example.com/wiki", saved.Url);
    }

    [Fact]
    public async Task FailedStoragePreservesSavedVersionAndDraftAndBlocksConcurrentEdits()
    {
        var store = new ProfileStore();
        var model = new ProfilesViewModel(store);
        await model.InitializeAsync();
        model.DisplayName = "Original";
        await model.SaveAsync();
        model.DisplayName = "Borrador";
        store.FailSave = true;
        await model.SaveAsync();
        Assert.Equal("Original", Assert.Single(model.Profiles).DisplayName);
        Assert.Equal("Borrador", model.DisplayName);
        Assert.True(model.HasError);
        model.RequestDelete();
        await model.DeleteAsync();
        Assert.Single(model.Profiles);
        store.FailSave = false;
        store.Pending = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var save = model.SaveAsync();
        Assert.False(model.CanEdit);
        model.NewProfile();
        await model.SaveAsync();
        Assert.NotNull(model.SelectedProfile);
        store.Pending.SetResult();
        await save;
        Assert.Equal("Borrador", Assert.Single(model.Profiles).DisplayName);
        Assert.True(model.CanEdit);
    }

    [Fact]
    public async Task FailedLoadCannotBeOverwrittenOrApplied()
    {
        var store = new ProfileStore { FailLoad = true };
        var model = new ProfilesViewModel(store);
        await model.InitializeAsync();
        model.DisplayName = "No guardar";
        await model.SaveAsync();
        var applied = false;
        await model.ApplyAsync(_ => { applied = true; return Task.CompletedTask; });
        Assert.False(model.CanEdit);
        Assert.False(applied);
        Assert.Equal(0, store.Saves);
        Assert.True(model.HasError);
    }

    [Fact]
    public async Task ApplyMonitorUsesExactPreferenceAndPreservesReviewAndSameMonitorRules()
    {
        var monitors = new Monitors();
        var store = new SettingsStore();
        var model = new MainWindowViewModel(monitors, store, new MonitorSelectionService());
        await model.InitializeAsync();
        var initial = model.SelectedCompanionMonitor;
        await Assert.ThrowsAsync<InvalidDataException>(() => model.ApplyProfileMonitorAsync("missing"));
        Assert.Equal(initial, model.SelectedCompanionMonitor);
        await Assert.ThrowsAsync<InvalidDataException>(() => model.ApplyProfileMonitorAsync("DISPLAY1"));
        Assert.False(model.AllowSameMonitorForTesting);
        model.RequireSelectionReview("review");
        await Assert.ThrowsAsync<InvalidDataException>(() => model.ApplyProfileMonitorAsync("DISPLAY3"));
        Assert.True(model.IsSelectionReviewRequired);
        model.ConfirmSelectionReview();
        store.FailSave = true;
        await Assert.ThrowsAsync<IOException>(() => model.ApplyProfileMonitorAsync("DISPLAY3"));
        Assert.Equal(initial, model.SelectedCompanionMonitor);
        store.FailSave = false;
        await model.ApplyProfileMonitorAsync("display3");
        Assert.Equal("DISPLAY3", model.SelectedCompanionMonitor!.DeviceName);
        Assert.Equal("DISPLAY3", store.Settings.CompanionMonitorDeviceName);
        await model.ApplyProfileMonitorAsync(null);
        Assert.Equal("DISPLAY3", model.SelectedCompanionMonitor.DeviceName);
        model.AllowSameMonitorForTesting = true;
        await model.ApplyProfileMonitorAsync("DISPLAY1");
        Assert.Equal("DISPLAY1", model.SelectedCompanionMonitor.DeviceName);
    }

    private sealed class ProfileStore : IGameProfileStore
    {
        public string FilePath => "memory";
        public GameProfileDocument Document { get; private set; } = new();
        public bool FailLoad { get; init; }
        public bool FailSave { get; set; }
        public int Saves { get; private set; }
        public TaskCompletionSource? Pending { get; set; }
        public Task<GameProfileDocument> LoadAsync(CancellationToken cancellationToken = default) =>
            FailLoad ? Task.FromException<GameProfileDocument>(new InvalidDataException()) : Task.FromResult(Document);
        public async Task SaveAsync(GameProfileDocument document, CancellationToken cancellationToken = default)
        {
            Saves++;
            if (FailSave) throw new IOException();
            if (Pending is not null) await Pending.Task;
            Document = document;
        }
    }
    private sealed class Monitors : IMonitorService
    {
        public IReadOnlyList<MonitorProfile> GetMonitors() => Enumerable.Range(1, 3).Select(i =>
            new MonitorProfile($"DISPLAY{i}", new DisplayRect((i - 1) * 1920, 0, 1920, 1080),
                new DisplayRect((i - 1) * 1920, 0, 1920, 1040), i == 1)).ToList();
    }
    private sealed class SettingsStore : IApplicationSettingsStore
    {
        public string FilePath => "memory";
        public ApplicationSettings Settings { get; private set; } = new();
        public bool FailSave { get; set; }
        public Task<ApplicationSettings> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(Settings);
        public Task SaveAsync(ApplicationSettings settings, CancellationToken cancellationToken = default)
        {
            if (FailSave) throw new IOException();
            Settings = settings;
            return Task.CompletedTask;
        }
    }
}
