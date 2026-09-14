using System.IO;
using System.Windows.Controls;
using GameTouchCompanion.App;
using GameTouchCompanion.Core;
using GameTouchCompanion.Native;

namespace GameTouchCompanion.IntegrationTests;

[Collection("Language state")]
public sealed class AutomaticOpeningTests
{
    [DesktopBrowserFact]
    [Trait("Category", "BrowserRuntime")]
    public async Task SavedStartupPreferenceEnablesDetectionOnceWithoutOpeningWithoutGame()
    {
        await BrowserRuntimeTests.RunOnStaAsync(async () =>
        {
            var folder = Path.Combine(AppContext.BaseDirectory, "StartupSmoke", Guid.NewGuid().ToString("N"));
            var settings = new TestSettings(true);
            var main = new MainWindow(new TestMonitors(), settings,
                new JsonBrowserSettingsStore(Path.Combine(folder, "browser.json")),
                new JsonGameProfileStore(Path.Combine(folder, "profiles.json")), new ControlledSource(), Path.Combine(folder, "webview-profile"));
            try
            {
                main.ShowActivated = false;
                main.Show();
                var toggle = (CheckBox)main.FindName("DetectionEnabledCheck");
                await BrowserRuntimeTests.WaitUntilAsync(() => toggle.IsChecked == true, () => "Startup detection was not enabled.");
                Assert.Null(main.CurrentCompanion);
                toggle.IsChecked = false;
                main.RaiseEvent(new System.Windows.RoutedEventArgs(System.Windows.FrameworkElement.LoadedEvent));
                Assert.False(toggle.IsChecked);
                Assert.True((await settings.LoadAsync()).EnableDetectionOnStartup);
            }
            finally { main.Close(); }
        }).WaitAsync(TimeSpan.FromSeconds(60));
    }

    [DesktopBrowserFact]
    [Trait("Category", "BrowserRuntime")]
    public async Task AutomaticPathRejectsUnsafeStatesAndOpensRealCompanionWithoutChangingBrowserPreferences()
    {
        await BrowserRuntimeTests.RunOnStaAsync(async () =>
        {
            var folder = Path.Combine(AppContext.BaseDirectory, "DetectionSmoke", Guid.NewGuid().ToString("N"));
            var monitorService = new TestMonitors();
            var profile = new GameProfile { DisplayName = "Test game", ProcessName = "TestGame.exe", AutoLaunch = true, CompanionMonitor = "TEST_COMPANION" };
            var profileStore = new JsonGameProfileStore(Path.Combine(folder, "profiles.json"));
            await profileStore.SaveAsync(new GameProfileDocument { Profiles = [profile] });
            var source = new ControlledSource();
            var main = new MainWindow(monitorService, new TestSettings(),
                new JsonBrowserSettingsStore(Path.Combine(folder, "browser.json")), profileStore, source, Path.Combine(folder, "webview-profile"));
            try
            {
                main.ShowActivated = false;
                main.Show();
                var editor = (ProfilesViewModel)((ProfilesPanel)main.FindName("GameProfilesPanel")).DataContext;
                var monitors = (MainWindowViewModel)main.DataContext;
                var browser = (BrowserViewModel)((BrowserSettingsPanel)main.FindName("BrowserPanel")).DataContext;
                await BrowserRuntimeTests.WaitUntilAsync(() => editor.CanEdit && monitors.CanOpenCompanion, () => "Configuration did not initialize.");
                Assert.False(((CheckBox)main.FindName("DetectionEnabledCheck")).IsChecked);
                var tabs = (TabControl)main.FindName("ConfigurationTabs");
                Assert.Equal(8, tabs.Items.Count);
                Assert.Same(main.FindName("SetupTab"), tabs.SelectedItem);
                Assert.Null(main.FindName("SetupNext"));
                Assert.NotEmpty(((TextBlock)main.FindName("SetupSummary")).Text);
                Assert.Null(main.CurrentCompanion);
                Assert.False(((CheckBox)main.FindName("DetectionEnabledCheck")).IsChecked);
                tabs.SelectedItem = main.FindName("ScreensTab");
                main.UpdateLayout();
                Assert.Equal(2, ((Canvas)main.FindName("MonitorPreviewCanvas")).Children.Count);
                tabs.SelectedItem = main.FindName("DiagnosticsTab");
                Assert.Contains("Foreground HWND", ((TextBlock)main.FindName("DiagnosticsSnapshot")).Text);
                Assert.Null(main.CurrentCompanion);
                tabs.SelectedItem = main.FindName("ProfilesTab");
                var panel = (ProfilesPanel)main.FindName("GameProfilesPanel");
                var list = (ListBox)panel.FindName("ProfileList");
                profile = Assert.Single(editor.Profiles);
                list.SelectedItem = profile;
                editor.DisplayName = "Unsaved UI draft";
                list.SelectedItem = null;
                await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                Assert.True(editor.DiscardConfirmation);
                Assert.Equal(profile, editor.SelectedProfile);
                editor.CancelDiscard();
                await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                Assert.Equal(profile, list.SelectedItem);
                Assert.Equal("Unsaved UI draft", editor.DisplayName);
                editor.NewProfile(); editor.ConfirmDiscard();
                Assert.Null(editor.SelectedProfile);
                Assert.False(editor.HasUnsavedChanges);
                // The overview reflects pending monitor review without clearing it.
                monitors.RequireSelectionReview("UX review test");
                Assert.Contains(Localization.Get("Message004"), ((TextBlock)main.FindName("SetupSummary")).Text);
                Assert.True(monitors.IsSelectionReviewRequired);
                monitors.ConfirmSelectionReview();
                // Render Configuration surfaces for layout review, without screen capture or personal data.
                foreach (TabItem tab in tabs.Items)
                {
                    tabs.SelectedItem = tab;
                    main.UpdateLayout();
                    await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                    var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap((int)main.ActualWidth, (int)main.ActualHeight,
                        96, 96, System.Windows.Media.PixelFormats.Pbgra32);
                    bitmap.Render(main);
                    var png = new System.Windows.Media.Imaging.PngBitmapEncoder();
                    png.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
                    using var output = File.Create(Path.Combine(folder, $"configuration-{tab.Name}.png"));
                    png.Save(output);
                }
                var process = new GameProcessInfo(123, "TestGame.exe", 123456);
                var window = new ProcessWindowInfo(456, 123, new DisplayRect(0, 0, 500, 500), true, false, false, false);
                var game = new DetectedGame(profile, process, window);

                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => main.OpenDetectedGameAsync(game, new CancellationToken(true)));
                Assert.Null(main.CurrentCompanion);
                monitors.RequireSelectionReview("Do not bypass this notice.");
                await Assert.ThrowsAsync<InvalidDataException>(() => main.OpenDetectedGameAsync(game, CancellationToken.None));
                Assert.True(monitors.IsSelectionReviewRequired);
                monitors.ConfirmSelectionReview();
                foreach (var target in new[] { "MISSING", "TEST_GAME" })
                {
                    var unsafeProfile = profile with { CompanionMonitor = target };
                    editor.Profiles[0] = unsafeProfile;
                    await Assert.ThrowsAsync<InvalidDataException>(() => main.OpenDetectedGameAsync(game with { Profile = unsafeProfile }, CancellationToken.None));
                    Assert.Null(main.CurrentCompanion);
                    Assert.Empty(browser.CurrentUrl);
                }
                editor.Profiles[0] = profile;
                source.Current = false;
                await Assert.ThrowsAsync<InvalidDataException>(() => main.OpenDetectedGameAsync(game, CancellationToken.None));
                Assert.Null(main.CurrentCompanion);
                source.Current = true;
                await main.OpenDetectedGameAsync(game, CancellationToken.None);
                Assert.NotNull(main.CurrentCompanion);
                await BrowserRuntimeTests.WaitUntilAsync(() => browser.IsReady && browser.CurrentUrl == profile.Url, () => browser.Status);
                Assert.Equal(BrowserUrlPolicy.LocalHomeUrl, browser.HomeUrl);
                Assert.Empty(browser.Favorites);
                Assert.Equal("TEST_COMPANION", monitors.SelectedCompanionMonitor!.DeviceName);
                main.CurrentCompanion!.Close();
                Assert.Null(main.CurrentCompanion);
                Assert.False(((CheckBox)main.FindName("DetectionEnabledCheck")).IsChecked);
            }
            finally { main.Close(); }
        }).WaitAsync(TimeSpan.FromSeconds(90));
    }

    // Simulated game/monitors isolate automation policy; real WPF/WebView2 are exercised, not physical focus.
    private sealed class ControlledSource : IGameDetectionSource
    {
        public bool Current { get; set; } = true;
        public GameDetectionSnapshot Capture(IReadOnlyCollection<string> names) => new([], [], 0);
        public bool IsStillForeground(DetectedGame game) => Current;
    }
    private sealed class TestMonitors : IMonitorService
    {
        public IReadOnlyList<MonitorProfile> GetMonitors() =>
        [
            new("TEST_GAME", new DisplayRect(0, 0, 600, 600), new DisplayRect(0, 0, 600, 600), true),
            new("TEST_COMPANION", new DisplayRect(600, 0, 800, 700), new DisplayRect(600, 0, 800, 700), false),
        ];
    }
    private sealed class TestSettings(bool startup = false) : IApplicationSettingsStore
    {
        private ApplicationSettings value = new() { GameMonitorDeviceName = "TEST_GAME", CompanionMonitorDeviceName = "TEST_COMPANION", AllowSameMonitorForTesting = true, EnableDetectionOnStartup = startup };
        public string FilePath => "memory";
        public Task<ApplicationSettings> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(value);
        public Task SaveAsync(ApplicationSettings settings, CancellationToken cancellationToken = default) { value = settings; return Task.CompletedTask; }
    }
}
