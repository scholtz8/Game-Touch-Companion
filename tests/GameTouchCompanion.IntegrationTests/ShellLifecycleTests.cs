using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GameTouchCompanion.App;
using GameTouchCompanion.Core;
using AppLocalization = GameTouchCompanion.App.Localization;

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
                Assert.True(main.IsDetectionRunning);
                Assert.Equal(0, registration.Writes);
                tray.Open!();
                Assert.True(main.IsVisible);
                main.Close();
                Assert.False(main.IsVisible);
                Assert.False(tray.Disposed);
                Assert.True(main.IsDetectionRunning);
                tray.Open!();
                Assert.True(main.IsDetectionRunning);
                main.PrepareForSessionEnd();
                main.Close();
                Assert.True(tray.Disposed);
            }
            finally { main.ExitCompletely(); }

            var secondTray = new FakeTray();
            var second = Create(secondTray, new FakeRegistration());
            try
            {
                // A manual launch must show Configuration even when the saved
                // StartMinimizedToTray preference is enabled. Hidden startup is
                // reserved for the explicit Windows --startup path.
                await second.StartShellAsync(launchedFromWindows: false);
                Assert.True(second.IsVisible);
                Assert.True(second.ShowActivated);
                Assert.True(second.IsDetectionRunning);
                secondTray.Exit!();
                Assert.True(secondTray.Disposed);
            }
            finally { second.ExitCompletely(); }
        }).WaitAsync(TimeSpan.FromSeconds(60));
    }


    [DesktopBrowserFact]
    [Trait("Category", "BrowserRuntime")]
    public async Task GeneralSettingsTieHiddenStartupToWindowsAndKeepMaintenanceActionSeparate()
    {
        await BrowserRuntimeTests.RunOnStaAsync(async () =>
        {
            var tray = new FakeTray();
            var registration = new FakeRegistration();
            var languageFile = Path.Combine(AppContext.BaseDirectory, "ShellSmoke", Guid.NewGuid().ToString("N"), "language.json");
            var main = Create(tray, registration, new LanguagePreferenceStore(languageFile));
            try
            {
                await main.StartShellAsync(launchedFromWindows: false, forceShow: true);

                var windowsStartup = (CheckBox)main.FindName("WindowsStartupCheck");
                var startHidden = (CheckBox)main.FindName("StartInTrayCheck");
                var closeToTray = (CheckBox)main.FindName("CloseToTrayCheck");
                var registerCopy = (Button)main.FindName("RegisterCurrentCopyButton");
                var save = (Button)main.FindName("SaveGeneralSettingsButton");
                var registrationStatus = (TextBlock)main.FindName("WindowsStartupStatus");

                Assert.False(windowsStartup.IsChecked == true);
                Assert.False(startHidden.IsEnabled);
                Assert.False(startHidden.IsChecked == true);
                Assert.True(closeToTray.IsEnabled);
                Assert.NotNull(registerCopy);
                Assert.False(registerCopy.IsEnabled);
                Assert.NotNull(save);

                windowsStartup.IsChecked = true;
                Assert.True(startHidden.IsEnabled);
                Assert.True(registerCopy.IsEnabled);
                startHidden.IsChecked = true;

                windowsStartup.IsChecked = false;
                Assert.False(startHidden.IsEnabled);
                Assert.False(startHidden.IsChecked == true);
                Assert.False(registerCopy.IsEnabled);

                main.SettingsTab.IsSelected = true;
                main.UpdateLayout();
                var buttonLabels = FindVisualChildren<Button>(main)
                    .Select(button => button.Content?.ToString() ?? string.Empty)
                    .ToArray();
                Assert.DoesNotContain(AppLocalization.Get("Ui044"), buttonLabels); // old Check registration action
                Assert.DoesNotContain(AppLocalization.Get("Ui047"), buttonLabels); // old Hide to tray now action
                Assert.DoesNotContain(AppLocalization.Get("Ui048"), buttonLabels); // old Exit completely action in Settings
                Assert.Contains(AppLocalization.Get("Ui043"), buttonLabels);       // maintenance registration action

                windowsStartup.IsChecked = true;
                registerCopy.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                Assert.True(registration.Exists);
                Assert.Equal(Visibility.Visible, registrationStatus.Visibility);

                startHidden.IsChecked = true;
                save.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await BrowserRuntimeTests.WaitUntilAsync(() => save.IsEnabled, () => "General settings save did not finish");
                Assert.True(registration.Exists);
                Assert.True(startHidden.IsEnabled);
                Assert.True(startHidden.IsChecked == true);

                windowsStartup.IsChecked = false;
                Assert.False(startHidden.IsChecked == true);
                save.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await BrowserRuntimeTests.WaitUntilAsync(() => save.IsEnabled, () => "General settings save did not finish");
                Assert.False(registration.Exists);
                Assert.False(startHidden.IsEnabled);
            }
            finally { main.ExitCompletely(); }
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
                    Assert.True(main.IsDetectionRunning);
                    main.Close();
                    Assert.True(tray.Disposed);
                }
                finally { main.ExitCompletely(); }
            }
        }).WaitAsync(TimeSpan.FromSeconds(60));
    }

    [DesktopBrowserFact]
    [Trait("Category", "BrowserRuntime")]
    public async Task RealNotifyIconCanLoadResourceAndDisposeOnDesktop()
    {
        await BrowserRuntimeTests.RunOnStaAsync(() =>
        {
            using var tray = new TrayService();
            tray.Initialize(() => { }, () => { }, () => { });
            Assert.True(tray.IsAvailable);
            Assert.Contains(AppLocalization.Get("TrayRearm"), tray.MenuLabels);
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

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject root) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match) yield return match;
            foreach (var descendant in FindVisualChildren<T>(child)) yield return descendant;
        }
    }

    private sealed class FakeTray : ITrayService
    {
        public bool Available { get; init; } = true;
        public bool FailInitialize { get; init; }
        public bool IsAvailable => Available && !Disposed;
        public bool Disposed { get; private set; }
        public Action? Open, Rearm, Exit;

        public void Initialize(Action openConfiguration, Action rearm, Action exit)
        {
            if (FailInitialize) throw new InvalidOperationException("Simulated tray failure");
            Open = openConfiguration;
            Rearm = rearm;
            Exit = exit;
        }

        public void Dispose() => Disposed = true;
    }

    private sealed class FakeRegistration : IStartupRegistrationService
    {
        public int Writes { get; private set; }
        public bool Exists { get; private set; }
        public StartupRegistration Read() => new(Exists, Exists);
        public void SetEnabled(bool enabled, bool replaceExisting = false)
        {
            Writes++;
            Exists = enabled;
        }
    }

    private sealed class Monitors : GameTouchCompanion.Native.IMonitorService
    {
        public IReadOnlyList<MonitorProfile> GetMonitors() =>
        [new("GAME", new(0, 0, 1000, 800), new(0, 0, 1000, 800), true),
         new("TOUCH", new(1000, 0, 800, 600), new(1000, 0, 800, 600), false)];
    }

    private sealed class Settings : IApplicationSettingsStore
    {
        private ApplicationSettings value = new() { StartMinimizedToTray = true, CloseToTray = true };
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
