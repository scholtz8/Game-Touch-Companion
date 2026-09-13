using System.Text.Json;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using GameTouchCompanion.Core;
using GameTouchCompanion.Native;
using Microsoft.Web.WebView2.Core;
using Serilog;

namespace GameTouchCompanion.App;

public partial class MainWindow : Window
{
    private readonly MainWindowViewModel viewModel;
    private readonly BrowserViewModel browserViewModel;
    private readonly ProfilesViewModel profilesViewModel;
    private readonly string? browserUserDataFolder;
    internal CompanionWindow? CurrentCompanion => companion;
    private readonly SemaphoreSlim settingsOperationGate = new(1, 1);
    private CompanionWindow? companion;
    private HwndSource? windowSource;
    private CancellationTokenSource? topologyChangeCancellation;
    private bool initializationStarted;
    private bool runtimeAvailable;

    public MainWindow() : this(new Win32MonitorService(), new JsonApplicationSettingsStore(),
        new JsonBrowserSettingsStore(), new JsonGameProfileStore(),
        new WindowsGameDetectionSource(new Win32ProcessWindowService()),
        startup: new WindowsStartupRegistrationService(Environment.ProcessPath ?? string.Empty), trayService: new TrayService()) { }

    internal MainWindow(IMonitorService monitorService, IApplicationSettingsStore monitorStore,
        IBrowserSettingsStore browserStore, IGameProfileStore profileStore, IGameDetectionSource source,
        string? userDataFolder = null, IStartupRegistrationService? startup = null, ITrayService? trayService = null,
        LanguagePreferenceStore? languagePreferences = null)
    {
        detectionSource = source;
        browserUserDataFolder = userDataFolder;
        startupRegistration = startup;
        tray = trayService;
        languageStore = languagePreferences ?? Localization.CreateStore();
        InitializeComponent();
        LanguageCombo.SelectedValue = Localization.Language;
        viewModel = new MainWindowViewModel(
            monitorService,
            monitorStore,
            new MonitorSelectionService());
        DataContext = viewModel;
        browserViewModel = new BrowserViewModel(browserStore);
        BrowserPanel.DataContext = browserViewModel;
        profilesViewModel = new ProfilesViewModel(profileStore);
        GameProfilesPanel.DataContext = profilesViewModel;
        GameProfilesPanel.ApplyProfile = ApplyProfileAsync;
        InitializeConfigurationUx();
        PropertyChangedEventManager.AddHandler(Localization.State, LanguageChanged, string.Empty);

        Loaded += MainWindow_Loaded;
        SourceInitialized += MainWindow_SourceInitialized;
        Closed += MainWindow_Closed;
        Closing += MainWindow_Closing;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e) => await InitializeConfigurationAsync();

    private async Task InitializeConfigurationAsync()
    {
        if (initializationStarted) return;
        initializationStarted = true;
        CheckRuntime();
        await InitializeMonitorsAsync();
        await browserViewModel.InitializeAsync();
        await profilesViewModel.InitializeAsync();
        if (isClosed) return;
        configurationReady = true;
        InitializeShell();
        if (runtimeAvailable && viewModel.SettingsLoaded && viewModel.EnableDetectionOnStartup &&
            viewModel.CanOpenCompanion && viewModel.ValidateCurrentSelection().IsValid &&
            !viewModel.IsSelectionReviewRequired && browserViewModel.SettingsLoaded && profilesViewModel.CanEdit)
            DetectionEnabledCheck.IsChecked = true;
        UpdateSetupGuide();
        DrawMonitorPreview();
    }

    private void MainWindow_SourceInitialized(object? sender, EventArgs e)
    {
        windowSource = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
        windowSource?.AddHook(WindowProc);
    }

    private async Task ApplyProfileAsync(GameProfile profile)
    {
        PauseDetection("La aplicación manual de un perfil pausó la detección. Puedes reactivarla desde Detección.");
        var validated = GameProfileValidation.Normalize(profile);
        if (!viewModel.CanOpenCompanion || viewModel.IsSelectionReviewRequired)
            throw new InvalidDataException("Revisa y confirma la selección en Pantallas antes de aplicar un perfil.");
        ConfigurationTabs.IsEnabled = false;
        await settingsOperationGate.WaitAsync();
        try
        {
            // Fresh topology before applying; preserves the existing disconnect/review lifecycle.
            await RefreshMonitorsUnderGateAsync("profile application");
            await viewModel.ApplyProfileMonitorAsync(validated.CompanionMonitor);
            if (companion is not null && viewModel.SelectedCompanionMonitor is not null)
                companion.PlaceOnMonitor(viewModel.SelectedCompanionMonitor);
            browserViewModel.Navigate(validated.Url);
            Log.Information("Saved game profile applied manually; detection paused");
        }
        finally { settingsOperationGate.Release(); ConfigurationTabs.IsEnabled = true; }
    }

    private void MainWindow_Closed(object? sender, EventArgs e)
    {
        isClosed = true;
        PropertyChangedEventManager.RemoveHandler(Localization.State, LanguageChanged, string.Empty);
        tray?.Dispose();
        configurationReady = false;
        viewModel.PropertyChanged -= ConfigurationStateChanged;
        viewModel.Monitors.CollectionChanged -= MonitorsForPreviewChanged;
        profilesViewModel.Profiles.CollectionChanged -= ProfilesForGuideChanged;
        detectionCancellation?.Cancel();
        detectionCancellation?.Dispose();
        topologyChangeCancellation?.Cancel();
        topologyChangeCancellation?.Dispose();
        companion?.Close();
        if (windowSource is not null)
            windowSource.RemoveHook(WindowProc);
    }

    private void CheckRuntime()
    {
        try
        {
            var version = CoreWebView2Environment.GetAvailableBrowserVersionString();
            runtimeAvailable = true;
            Localization.Text(RuntimeStatus, () => Localization.F($"WebView2 Runtime: {version}"));
            Log.Information("WebView2 Runtime={Version}", version);
        }
        catch (WebView2RuntimeNotFoundException ex)
        {
            Localization.Text(RuntimeStatus, () => Localization.T("WebView2 Runtime not installed."));
            Log.Error(ex, "WebView2 Runtime not found");
            AppDialog.Show(Localization.T("Microsoft Edge WebView2 Evergreen Runtime is required. Install it from Microsoft, then restart the application."),
                Localization.T("WebView2 Runtime missing"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task InitializeMonitorsAsync()
    {
        await settingsOperationGate.WaitAsync();
        try
        {
            await viewModel.InitializeAsync();
            LogCurrentMonitors("startup");
        }
        catch (JsonException ex)
        {
            Log.Error(ex, "Settings file is invalid: {SettingsPath}", JsonApplicationSettingsStore.GetDefaultFilePath());
            viewModel.ReportError(LocalizedMessage.Format($"Settings JSON is invalid. Correct or remove: {JsonApplicationSettingsStore.GetDefaultFilePath()}"));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Monitor initialization failed");
            viewModel.ReportError(LocalizedMessage.Format($"Monitor detection failed: {ex.Message}"));
        }
        finally
        {
            settingsOperationGate.Release();
        }
    }

    private async void MonitorSelection_Changed(object sender, RoutedEventArgs e)
    {
        if (viewModel.IsUpdating) return;

        // Deferred binding/template events must not clear the other selector or persist an initial null.
        var selected = (sender as ComboBox)?.SelectedItem as MonitorProfile;
        var sameMonitor = AllowSameMonitorCheck.IsChecked == true;
        if (ReferenceEquals(sender, GameMonitorCombo) && (selected is null || selected == viewModel.SelectedGameMonitor)) return;
        if (ReferenceEquals(sender, CompanionMonitorCombo) && (selected is null || selected == viewModel.SelectedCompanionMonitor)) return;
        if (ReferenceEquals(sender, AllowSameMonitorCheck) && sameMonitor == viewModel.AllowSameMonitorForTesting) return;

        await settingsOperationGate.WaitAsync();
        try
        {
            if (isClosed) return;
            if (ReferenceEquals(sender, GameMonitorCombo)) viewModel.SelectedGameMonitor = selected;
            else if (ReferenceEquals(sender, CompanionMonitorCombo)) viewModel.SelectedCompanionMonitor = selected;
            else if (ReferenceEquals(sender, AllowSameMonitorCheck)) viewModel.AllowSameMonitorForTesting = sameMonitor;
            await viewModel.ApplySelectionAsync();
            if (viewModel.IsSelectionReviewRequired)
                viewModel.ConfirmSelectionReview();
            if (companion is not null && viewModel.SelectedCompanionMonitor is not null)
                companion.PlaceOnMonitor(viewModel.SelectedCompanionMonitor);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not save monitor selection");
            viewModel.ReportError(LocalizedMessage.Format($"Could not save monitor selection: {ex.Message}"));
        }
        finally
        {
            settingsOperationGate.Release();
        }
    }

    private async void RefreshMonitors_Click(object sender, RoutedEventArgs e) =>
        await RefreshMonitorsAsync("manual refresh");

    private void OpenCompanion_Click(object sender, RoutedEventArgs e)
    {
        if (!viewModel.CanOpenCompanion) return;
        var validation = viewModel.ValidateCurrentSelection();
        if (!validation.IsValid || viewModel.SelectedCompanionMonitor is null)
        {
            AppDialog.Show(string.Join(Environment.NewLine, validation.Issues.Select(static issue => Localization.MonitorIssue(issue))),
                Localization.T("Invalid monitor selection"), MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (validation.Issues.Any(static issue => issue.Code == MonitorSelectionIssueCode.SameMonitorOverrideEnabled))
            Log.Warning("Opening Companion on the same monitor because the explicit testing override is enabled");

        ShowCompanionOnSelectedMonitor();
    }

    private void ShowCompanionOnSelectedMonitor()
    {
        if (isClosed || !viewModel.CanOpenCompanion || viewModel.SelectedCompanionMonitor is null) return;
        if (companion is null)
        {
            companion = new CompanionWindow(viewModel.SelectedCompanionMonitor, browserViewModel, browserUserDataFolder);
            companion.Closed += (_, _) =>
            {
                companion = null;
                if (!isClosed) PauseDetection("Companion cerrado; detección pausada para evitar reapertura. Reactiva y Rearma cuando quieras otro intento.");
            };
        }
        else
        {
            companion.PlaceOnMonitor(viewModel.SelectedCompanionMonitor);
        }

        companion.ShowWithoutActivation();
        Log.Information("Companion requested on {Monitor} at {Bounds}",
            viewModel.SelectedCompanionMonitor.DeviceName,
            viewModel.SelectedCompanionMonitor.Bounds);
    }

    private void BrowserPanel_OpenCompanionRequested(object? sender, EventArgs e)
    {
        if (!viewModel.CanOpenCompanion)
        {
            browserViewModel.ReportError("Revisa la selección y los avisos de la pestaña Pantallas antes de abrir Companion.");
            return;
        }
        OpenCompanion_Click(this, new RoutedEventArgs());
    }

    private void ConfirmSelectionReview_Click(object sender, RoutedEventArgs e)
    {
        if (viewModel.ConfirmSelectionReview())
            Log.Information("User confirmed fallback monitor selection after topology change");
    }

    private nint WindowProc(nint hwnd, int message, nint wParam, nint lParam, ref bool handled)
    {
        if (message is NativeConstants.WmDisplayChange or NativeConstants.WmDeviceChange)
        {
            ScheduleTopologyRefresh(message);
        }

        return nint.Zero;
    }

    private async Task RefreshMonitorsAsync(string reason)
    {
        await settingsOperationGate.WaitAsync();
        try
        {
            await RefreshMonitorsUnderGateAsync(reason);
        }
        finally { settingsOperationGate.Release(); }
    }

    private async Task RefreshMonitorsUnderGateAsync(string reason)
    {
        try
        {
            var previousCompanionDeviceName = viewModel.SelectedCompanionMonitor?.DeviceName;
            await viewModel.RefreshAsync();
            LogCurrentMonitors(reason);
            if (companion is null || viewModel.SelectedCompanionMonitor is null) return;

            var previousMonitorStillExists = previousCompanionDeviceName is not null &&
                viewModel.Monitors.Any(monitor => string.Equals(
                    monitor.DeviceName,
                    previousCompanionDeviceName,
                    StringComparison.OrdinalIgnoreCase));

            if (!previousMonitorStillExists)
            {
                Log.Warning("Selected Companion monitor {Monitor} disappeared; closing fullscreen Companion", previousCompanionDeviceName);
                companion.Close();
                viewModel.RequireSelectionReview(LocalizedMessage.Format($"The previous Companion monitor {previousCompanionDeviceName} is unavailable. Companion was closed; review the fallback selection before reopening it."));
                return;
            }

            companion.PlaceOnMonitor(viewModel.SelectedCompanionMonitor);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Monitor refresh failed. Reason={Reason}", reason);
            viewModel.ReportError(LocalizedMessage.Format($"Monitor refresh failed: {ex.Message}"));
        }
    }

    private void ScheduleTopologyRefresh(int message)
    {
        topologyChangeCancellation?.Cancel();
        topologyChangeCancellation?.Dispose();
        topologyChangeCancellation = new CancellationTokenSource();
        Log.Information("Display topology notification {Message:X}; scheduling monitor refresh", message);
        _ = RefreshTopologyAfterDelayAsync(topologyChangeCancellation.Token);
    }

    private async Task RefreshTopologyAfterDelayAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken);
            await RefreshMonitorsAsync("display topology changed");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private void LogCurrentMonitors(string reason)
    {
        foreach (var monitor in viewModel.Monitors)
        {
            Log.Information("Monitor detected. Reason={Reason}; Device={Device}; Bounds={Bounds}; WorkArea={WorkArea}; Primary={Primary}",
                reason, monitor.DeviceName, monitor.Bounds, monitor.WorkingArea, monitor.IsPrimary);
        }
    }
}
