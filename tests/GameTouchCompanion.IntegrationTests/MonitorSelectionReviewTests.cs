using GameTouchCompanion.App;
using GameTouchCompanion.Core;
using GameTouchCompanion.Native;

namespace GameTouchCompanion.IntegrationTests;

public sealed class MonitorSelectionReviewTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task TopologyNoticeSurvivesSubsequentMonitorRefresh()
    {
        var primary = Monitor("\\\\.\\DISPLAY1", 0, true);
        var removed = Monitor("\\\\.\\DISPLAY2", 1920);
        var fallback = Monitor("\\\\.\\DISPLAY3", -1920);
        var monitorService = new MutableMonitorService([primary, removed]);
        var store = new MemorySettingsStore(new ApplicationSettings
        {
            GameMonitorDeviceName = primary.DeviceName,
            CompanionMonitorDeviceName = removed.DeviceName,
        });
        var viewModel = new MainWindowViewModel(monitorService, store, new MonitorSelectionService());
        await viewModel.InitializeAsync();
        Assert.Null(store.Current.GameMonitorId);
        Assert.Null(store.Current.CompanionMonitorId);

        monitorService.Monitors = [primary, fallback];
        await viewModel.RefreshAsync();
        viewModel.RequireSelectionReview("DISPLAY2 disappeared; review the fallback.");

        await viewModel.RefreshAsync();

        Assert.True(viewModel.IsSelectionReviewRequired);
        Assert.False(viewModel.CanOpenCompanion);
        Assert.Equal("DISPLAY2 disappeared; review the fallback.", viewModel.SelectionReviewMessage);
        Assert.Equal(fallback, viewModel.SelectedCompanionMonitor);

        Assert.True(viewModel.ConfirmSelectionReview());
        Assert.False(viewModel.IsSelectionReviewRequired);
        Assert.True(viewModel.CanOpenCompanion);
    }


    [Fact]
    [Trait("Category", "Integration")]
    public async Task PersistentMonitorReconnectClearsTopologyReviewEvenWhenDisplayNumberChanges()
    {
        var primary = Monitor(@"\\.\DISPLAY1", 0, true, "MONITOR-GAME", "Game display");
        var touch = Monitor(@"\\.\DISPLAY2", 1920, false, "MONITOR-TOUCH", "Touch display");
        var fallback = Monitor(@"\\.\DISPLAY3", -1920, false, "MONITOR-OTHER", "Other display");
        var monitorService = new MutableMonitorService([primary, touch, fallback]);
        var store = new MemorySettingsStore(new ApplicationSettings
        {
            GameMonitorId = primary.StableId,
            CompanionMonitorId = touch.StableId,
            GameMonitorDeviceName = primary.DeviceName,
            CompanionMonitorDeviceName = touch.DeviceName,
        });
        var viewModel = new MainWindowViewModel(monitorService, store, new MonitorSelectionService());
        await viewModel.InitializeAsync();

        monitorService.Monitors = [primary, fallback];
        await viewModel.RefreshAsync();
        Assert.True(viewModel.IsSelectionReviewRequired);
        Assert.False(viewModel.CanOpenCompanion);

        var renumberedTouch = Monitor(@"\\.\DISPLAY4", 1920, false, "MONITOR-TOUCH", "Touch display");
        monitorService.Monitors = [primary, fallback, renumberedTouch];
        await viewModel.RefreshAsync();

        Assert.False(viewModel.IsSelectionReviewRequired);
        Assert.True(viewModel.CanOpenCompanion);
        Assert.Equal("MONITOR-TOUCH", viewModel.SelectedCompanionMonitor!.StableId);
        Assert.Equal(@"\\.\DISPLAY4", viewModel.SelectedCompanionMonitor.DeviceName);
    }



    [Fact]
    [Trait("Category", "Integration")]
    public async Task LegacyDisplayAliasesRequireOneExplicitConfirmationBeforePersistentIdsAreSaved()
    {
        var primary = Monitor(@"\\.\DISPLAY1", 0, true, "MONITOR-GAME", "Game display");
        var touch = Monitor(@"\\.\DISPLAY2", 1920, false, "MONITOR-TOUCH", "Touch display");
        var monitorService = new MutableMonitorService([primary, touch]);
        var store = new MemorySettingsStore(new ApplicationSettings
        {
            GameMonitorDeviceName = primary.DeviceName,
            CompanionMonitorDeviceName = touch.DeviceName,
        });
        var viewModel = new MainWindowViewModel(monitorService, store, new MonitorSelectionService());

        await viewModel.InitializeAsync();

        Assert.True(viewModel.IsSelectionReviewRequired);
        Assert.False(viewModel.CanOpenCompanion);
        Assert.Null(store.Current.GameMonitorId);
        Assert.Null(store.Current.CompanionMonitorId);
        Assert.Equal(primary, viewModel.SelectedGameMonitor);
        Assert.Equal(touch, viewModel.SelectedCompanionMonitor);

        await viewModel.ApplySelectionAsync();

        Assert.False(viewModel.IsSelectionReviewRequired);
        Assert.True(viewModel.CanOpenCompanion);
        Assert.Equal("MONITOR-GAME", store.Current.GameMonitorId);
        Assert.Equal("MONITOR-TOUCH", store.Current.CompanionMonitorId);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ExplicitlyApplyingDisplayedFallbackReplacesMissingPersistentIdentity()
    {
        var primary = Monitor(@"\\.\DISPLAY1", 0, true, "MONITOR-GAME", "Game display");
        var missingTouch = Monitor(@"\\.\DISPLAY2", 1920, false, "MONITOR-TOUCH", "Touch display");
        var fallback = Monitor(@"\\.\DISPLAY3", -1920, false, "MONITOR-OTHER", "Other display");
        var monitorService = new MutableMonitorService([primary, missingTouch]);
        var store = new MemorySettingsStore(new ApplicationSettings
        {
            GameMonitorId = primary.StableId,
            CompanionMonitorId = missingTouch.StableId,
            GameMonitorDeviceName = primary.DeviceName,
            CompanionMonitorDeviceName = missingTouch.DeviceName,
        });
        var viewModel = new MainWindowViewModel(monitorService, store, new MonitorSelectionService());
        await viewModel.InitializeAsync();

        monitorService.Monitors = [primary, fallback];
        await viewModel.RefreshAsync();

        Assert.True(viewModel.IsSelectionReviewRequired);
        Assert.Equal(fallback, viewModel.SelectedCompanionMonitor);
        Assert.Equal("MONITOR-TOUCH", store.Current.CompanionMonitorId);

        // Models the explicit confirmation button: save exactly what is currently displayed.
        await viewModel.ApplySelectionAsync();

        Assert.False(viewModel.IsSelectionReviewRequired);
        Assert.True(viewModel.CanOpenCompanion);
        Assert.Equal("MONITOR-OTHER", store.Current.CompanionMonitorId);
        Assert.Equal(@"\\.\DISPLAY3", store.Current.CompanionMonitorDeviceName);
    }

    private static MonitorProfile Monitor(string name, int x, bool primary = false, string? stableId = null, string? friendlyName = null) => new(
        name,
        new DisplayRect(x, 0, 1920, 1080),
        new DisplayRect(x, 0, 1920, 1040),
        primary,
        stableId,
        friendlyName);

    private sealed class MutableMonitorService(IReadOnlyList<MonitorProfile> monitors) : IMonitorService
    {
        public IReadOnlyList<MonitorProfile> Monitors { get; set; } = monitors;
        public IReadOnlyList<MonitorProfile> GetMonitors() => Monitors;
    }

    private sealed class MemorySettingsStore(ApplicationSettings settings) : IApplicationSettingsStore
    {
        private ApplicationSettings current = settings;
        public ApplicationSettings Current => current;
        public string FilePath => "memory://settings.json";
        public Task<ApplicationSettings> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(current);
        public Task SaveAsync(ApplicationSettings value, CancellationToken cancellationToken = default)
        {
            current = value;
            return Task.CompletedTask;
        }
    }
}
