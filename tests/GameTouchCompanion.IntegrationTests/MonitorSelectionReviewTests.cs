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

    private static MonitorProfile Monitor(string name, int x, bool primary = false) => new(
        name,
        new DisplayRect(x, 0, 1920, 1080),
        new DisplayRect(x, 0, 1920, 1040),
        primary);

    private sealed class MutableMonitorService(IReadOnlyList<MonitorProfile> monitors) : IMonitorService
    {
        public IReadOnlyList<MonitorProfile> Monitors { get; set; } = monitors;
        public IReadOnlyList<MonitorProfile> GetMonitors() => Monitors;
    }

    private sealed class MemorySettingsStore(ApplicationSettings settings) : IApplicationSettingsStore
    {
        private ApplicationSettings current = settings;
        public string FilePath => "memory://settings.json";
        public Task<ApplicationSettings> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(current);
        public Task SaveAsync(ApplicationSettings value, CancellationToken cancellationToken = default)
        {
            current = value;
            return Task.CompletedTask;
        }
    }
}
