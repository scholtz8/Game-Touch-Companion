using System.IO;
using System.Windows.Controls;
using GameTouchCompanion.App;
using GameTouchCompanion.Core;

namespace GameTouchCompanion.IntegrationTests;

[Collection("Language state")]
public sealed class ShellLifecycleTests
{
    internal static MainWindow CreateForLanguageTest(LanguagePreferenceStore store) => Create(new FakeTray(), new FakeRegistration(), store);
    [DesktopBrowserFact]
    [Trait("Category", "BrowserRuntime")]
    public async Task HiddenStartupRestoreCloseToTrayExitAndSessionEndAreDistinct()
    {
        await BrowserRuntimeTests.RunOnStaAsync(async () =>
        {
            var tray = new FakeTray();
            var registration = new FakeRegistration();
            var main = Create(tray, registration);
            try
            {
                await main.StartShellAsync(launchedFromWindows: true);
                Assert.False(main.IsVisible);
                Assert.False(main.ShowActivated);
                Assert.True(((CheckBox)main.FindName("DetectionEnabledCheck")).IsChecked);
                Assert.True(tray.Detection);
                Assert.Equal(0, registration.Writes);
                tray.Open!();
                Assert.True(main.IsVisible);
                main.Close();
                Assert.False(main.IsVisible);
                Assert.False(tray.Disposed);
                Assert.True(((CheckBox)main.FindName("DetectionEnabledCheck")).IsChecked);
                tray.Toggle!();
                Assert.False(tray.Detection);
                tray.Open!();
                Assert.False(((CheckBox)main.FindName("DetectionEnabledCheck")).IsChecked); // no reinitialization
                main.PrepareForSessionEnd();
                main.Close();
                Assert.True(tray.Disposed);
            }
            finally { main.ExitCompletely(); }

            var secondTray = new FakeTray();
            var second = Create(secondTray, new FakeRegistration());
            try
            {
                await second.StartShellAsync(false, forceShow: true);
                Assert.True(second.IsVisible);
                secondTray.Exit!();
                Assert.True(secondTray.Disposed);
            }
            finally { second.ExitCompletely(); }
        }).WaitAsync(TimeSpan.FromSeconds(60));
    }

    [DesktopBrowserFact]
    [Trait("Category", "BrowserRuntime")]
    public async Task MissingOrFailedTrayKeepsConfigurationAccessibleAndCloseExits()
    {
        await BrowserRuntimeTests.RunOnStaAsync(async () =>
        {
            foreach (var fail in new[] { false, true })
            {
                var tray = new FakeTray { Available = false, FailInitialize = fail };
                var main = Create(tray, new FakeRegistration());
                try
                {
                    await main.StartShellAsync(true);
                    Assert.True(main.IsVisible);
                    main.Close();
                    Assert.True(tray.Disposed);
                }
                finally { main.ExitCompletely(); }
            }
        }).WaitAsync(TimeSpan.FromSeconds(60));
    }

    [DesktopBrowserFact]
    [Trait("Category", "BrowserRuntime")]
    public async Task RealNotifyIconCanLoadResourceUpdateAndDisposeOnDesktop()
    {
        await BrowserRuntimeTests.RunOnStaAsync(() =>
        {
            using var tray = new TrayService();
            tray.Initialize(() => { }, () => { }, () => { }, () => { });
            Assert.True(tray.IsAvailable);
            tray.UpdateDetection(true);
            tray.UpdateDetection(false);
            tray.Dispose();
            Assert.False(tray.IsAvailable);
            return Task.CompletedTask;
        }).WaitAsync(TimeSpan.FromSeconds(30));
    }

    private static MainWindow Create(FakeTray tray, FakeRegistration registration, LanguagePreferenceStore? language = null)
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "ShellSmoke", Guid.NewGuid().ToString("N"));
        return new MainWindow(new Monitors(), new Settings(),
            new JsonBrowserSettingsStore(Path.Combine(folder, "browser.json")),
            new JsonGameProfileStore(Path.Combine(folder, "profiles.json")), new NoGame(),
            Path.Combine(folder, "webview"), registration, tray, language);
    }
    private sealed class FakeTray : ITrayService
    {
        public bool Available { get; init; } = true;
        public bool FailInitialize { get; init; }
        public bool IsAvailable => Available && !Disposed;
        public bool Disposed { get; private set; }
        public bool Detection { get; private set; }
        public Action? Open, Toggle, Exit;
        public void Initialize(Action openConfiguration, Action toggleDetection, Action rearm, Action exit)
        {
            if (FailInitialize) throw new InvalidOperationException("Simulated tray failure");
            Open = openConfiguration; Toggle = toggleDetection; Exit = exit;
        }
        public void UpdateDetection(bool enabled) => Detection = enabled;
        public void Dispose() => Disposed = true;
    }
    private sealed class FakeRegistration : IStartupRegistrationService
    {
        public int Writes { get; private set; }
        public StartupRegistration Read() => new(false, false);
        public void SetEnabled(bool enabled, bool replaceExisting = false) => Writes++;
    }
    private sealed class Monitors : GameTouchCompanion.Native.IMonitorService
    {
        public IReadOnlyList<MonitorProfile> GetMonitors() =>
        [new("GAME", new(0, 0, 1000, 800), new(0, 0, 1000, 800), true),
         new("TOUCH", new(1000, 0, 800, 600), new(1000, 0, 800, 600), false)];
    }
    private sealed class Settings : IApplicationSettingsStore
    {
        private ApplicationSettings value = new() { EnableDetectionOnStartup = true, StartMinimizedToTray = true, CloseToTray = true };
        public string FilePath => "memory";
        public Task<ApplicationSettings> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult(value);
        public Task SaveAsync(ApplicationSettings settings, CancellationToken cancellationToken = default) { value = settings; return Task.CompletedTask; }
    }
    private sealed class NoGame : IGameDetectionSource
    {
        public GameDetectionSnapshot Capture(IReadOnlyCollection<string> executableNames) => new([], [], 0);
        public bool IsStillForeground(DetectedGame game) => false;
    }
}
