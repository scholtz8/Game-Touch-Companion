using System.IO;
using System.Reflection;
using System.Windows;
using Serilog;

namespace GameTouchCompanion.App;

public partial class App : Application
{
    private SingleInstanceCoordinator? singleInstance;
    private bool pendingShowRequest;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var logDirectory = LogPathResolver.Resolve();
        Log.Logger = new LoggerConfiguration().MinimumLevel.Debug()
            .WriteTo.File(
                Path.Combine(logDirectory, "app-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                fileSizeLimitBytes: 10 * 1024 * 1024,
                rollOnFileSizeLimit: true,
                flushToDiskInterval: TimeSpan.FromSeconds(2))
            .CreateLogger();
        Log.Information("Logging initialized. Directory={LogDirectory}", logDirectory);
        Log.Information("Startup. Windows={Windows}; Runtime={Runtime}; AppVersion={AppVersion}",
            Environment.OSVersion,
            Environment.Version,
            Assembly.GetEntryAssembly()?.GetName().Version);

        var launchedFromWindows = e.Args.Contains("--startup", StringComparer.OrdinalIgnoreCase);
        var explicitShow = e.Args.Contains("--show", StringComparer.OrdinalIgnoreCase);
        singleInstance = new SingleInstanceCoordinator();
        if (!singleInstance.IsPrimary)
        {
            var shouldShowExisting = !launchedFromWindows || explicitShow;
            var signaled = !shouldShowExisting || await singleInstance.SignalPrimaryAsync("show");
            Log.Information("Secondary launch suppressed. WindowsStartup={WindowsStartup}; ShowExisting={ShowExisting}; Signaled={Signaled}",
                launchedFromWindows, shouldShowExisting, signaled);
            Shutdown(0);
            return;
        }

        singleInstance.StartListening(command => Dispatcher.InvokeAsync(() =>
        {
            if (!string.Equals(command, "show", StringComparison.OrdinalIgnoreCase)) return;
            if (MainWindow is MainWindow main) main.ShowConfigurationFromTray();
            else pendingShowRequest = true;
        }).Task);

        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        try
        {
            if (!await InitializeLanguageAsync(Localization.CreateStore(), store =>
            {
                var selection = new LanguageSelectionWindow(store);
                return selection.ShowDialog() == true ? selection.SelectedLanguage : null;
            }))
            {
                Shutdown();
                return;
            }

            await Appearance.Current.InitializeAsync();
            var main = new MainWindow();
            MainWindow = main;
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            SessionEnding += (_, _) => main.PrepareForSessionEnd();
            await main.StartShellAsync(launchedFromWindows, explicitShow || pendingShowRequest);
            if (pendingShowRequest)
            {
                pendingShowRequest = false;
                main.ShowConfigurationFromTray();
            }
        }
        catch (Exception ex)
        {
            Log.Error("Application initialization failed. Type={Type}; Code={Code}", ex.GetType().Name, ex.HResult);
            if (MainWindow is MainWindow main) main.ExitCompletely();
            MessageBox.Show("No se pudo iniciar Game Touch Companion. Revisa los logs locales. / Could not start Game Touch Companion. Check local logs.", "Game Touch Companion");
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        singleInstance?.Dispose();
        singleInstance = null;
        Log.Information("Shutdown");
        Log.CloseAndFlush();
        base.OnExit(e);
    }

    internal static async Task<bool> InitializeLanguageAsync(GameTouchCompanion.Core.LanguagePreferenceStore store,
        Func<GameTouchCompanion.Core.LanguagePreferenceStore, string?> choose)
    {
        var language = await store.LoadAsync() ?? choose(store);
        if (language is null) return false;
        Localization.Initialize(language);
        return true;
    }
}
