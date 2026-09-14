using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using GameTouchCompanion.Core;
using GameTouchCompanion.Native;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Serilog;

namespace GameTouchCompanion.App;

public partial class CompanionWindow : Window
{
    private sealed class TabRuntime(GameProfileTab definition) : INotifyPropertyChanged
    {
        private string title = BuildFallbackTabTitle(definition.Url);

        public GameProfileTab Definition { get; } = definition;
        public WebView2? View { get; set; }
        public CoreWebView2? Core { get; set; }
        public string CurrentUrl { get; set; } = definition.Url;
        public string? LastNavigationTarget { get; set; }
        public ulong NavigationId { get; set; }
        public bool RequiresReopen { get; set; }
        public string MessageToken { get; } = Guid.NewGuid().ToString("N");
        public string Title
        {
            get => title;
            set
            {
                var normalized = string.IsNullOrWhiteSpace(value) ? BuildFallbackTabTitle(CurrentUrl) : value.Trim();
                if (string.Equals(title, normalized, StringComparison.Ordinal)) return;
                title = normalized;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    private readonly BrowserViewModel viewModel;
    private readonly string userDataFolder;
    private readonly List<GameProfile> availableProfiles = [];
    private readonly List<GameProfileTab> sessionTabs = [];
    private readonly Dictionary<string, TabRuntime> tabs = new(StringComparer.OrdinalIgnoreCase);
    private MonitorProfile targetMonitor;
    private CoreWebView2Environment? environment;
    private nint hwnd;
    private HwndSource? windowSource;
    private bool isClosing;
    private bool isInitializing;
    private GameProfile? activeProfile;
    private TabRuntime? activeTab;

    public Func<GameProfile, Task>? SwitchProfileRequested { get; set; }

    public CompanionWindow(MonitorProfile targetMonitor, BrowserViewModel viewModel, string? userDataFolder = null)
    {
        this.targetMonitor = targetMonitor ?? throw new ArgumentNullException(nameof(targetMonitor));
        this.viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        this.userDataFolder = userDataFolder ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GameTouchCompanion", "WebView2");
        InitializeComponent();
        DataContext = viewModel;
        InitializeAppearance();
        viewModel.NavigationRequested += NavigateRequested;
        SourceInitialized += OnSourceInitialized;
        Loaded += OnLoaded;
    }

    public void SetAvailableProfiles(IEnumerable<GameProfile> profiles)
    {
        availableProfiles.Clear();
        availableProfiles.AddRange(profiles.Select(GameProfileValidation.Normalize));
        ProfilesList.ItemsSource = availableProfiles;
        ProfileSwitchAction.Visibility = availableProfiles.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    public async Task LoadProfileAsync(GameProfile profile)
    {
        var normalized = GameProfileValidation.Normalize(profile);
        activeProfile = normalized;
        ActiveProfileText.Text = normalized.DisplayName;
        ProfilesPanel.Visibility = Visibility.Collapsed;
        await ReplaceTabsAsync(normalized.Tabs, normalized.PrimaryTabId);
        Log.Information("Companion profile loaded. Profile={Profile}; Tabs={TabCount}; Primary={PrimaryTab}",
            normalized.DisplayName, normalized.Tabs.Count, normalized.PrimaryTab.Name);
    }

    public async Task LoadManualSessionAsync(string url)
    {
        if (!BrowserUrlPolicy.TryNormalize(url, out var normalized)) normalized = viewModel.HomeUrl;
        activeProfile = null;
        ActiveProfileText.Text = Localization.Get("Ui160");
        await ReplaceTabsAsync([new GameProfileTab { Id = "manual", Name = Localization.Get("Ui161"), Url = normalized }], "manual");
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        if (e.Cancel) return;
        isClosing = true;
        PropertyChangedEventManager.RemoveHandler(Appearance.Current, AppearanceChanged, string.Empty);
        hwnd = nint.Zero;
        viewModel.NavigationRequested -= NavigateRequested;
        SourceInitialized -= OnSourceInitialized;
        Loaded -= OnLoaded;
        windowSource?.RemoveHook(WindowProc);
        DisposeTabs();
        viewModel.ReportClosed();
    }

    public void ShowWithoutActivation()
    {
        ShowActivated = false;
        if (!IsVisible) Show();
        PlaceOnMonitor(targetMonitor);
    }

    public void PlaceOnMonitor(MonitorProfile monitor)
    {
        targetMonitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        if (isClosing || hwnd == nint.Zero) return;
        Win32WindowPlacementService.PlaceOnMonitor(hwnd, targetMonitor);
        Log.Information("Companion placed without activation. Monitor={Monitor}; Bounds={Bounds}", targetMonitor.DeviceName, targetMonitor.Bounds);
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        hwnd = new WindowInteropHelper(this).Handle;
        windowSource = HwndSource.FromHwnd(hwnd);
        windowSource?.AddHook(WindowProc);
        PlaceOnMonitor(targetMonitor);
        NoActivateWindowService.Apply(hwnd);
        Log.Information("Companion HWND={Hwnd:X}; WS_EX_NOACTIVATE applied", hwnd);
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await EnsureEnvironmentAsync();
            if (tabs.Count == 0) await LoadManualSessionAsync(viewModel.RequestedUrl ?? viewModel.HomeUrl);
        }
        catch (Exception ex)
        {
            LogBrowserFailure("initial-load", ex);
            ShowError("No se pudo iniciar WebView2. Revisa el Runtime y los logs.");
        }
    }

    private async Task EnsureEnvironmentAsync()
    {
        if (environment is not null || isInitializing || isClosing) return;
        isInitializing = true;
        try
        {
            _ = CoreWebView2Environment.GetAvailableBrowserVersionString();
            viewModel.ReportStatus("Iniciando navegador…");
            environment = await CoreWebView2Environment.CreateAsync(userDataFolder: userDataFolder);
            Log.Information("Shared WebView2 environment initialized. UserDataFolder={Folder}", userDataFolder);
        }
        catch (WebView2RuntimeNotFoundException ex)
        {
            LogBrowserFailure("runtime-check", ex);
            ShowError("Falta WebView2 Evergreen Runtime. Instálalo desde Microsoft y vuelve a abrir Companion.");
            throw;
        }
        finally { isInitializing = false; }
    }

    private async Task ReplaceTabsAsync(IEnumerable<GameProfileTab> definitions, string? primaryTabId)
    {
        DisposeTabs();
        var ordered = definitions.OrderBy(tab => tab.Order).ToList();
        sessionTabs.Clear();
        sessionTabs.AddRange(ordered);
        foreach (var definition in ordered) tabs[definition.Id] = new TabRuntime(definition);
        RefreshTabList();
        var target = ordered.FirstOrDefault(tab => string.Equals(tab.Id, primaryTabId, StringComparison.OrdinalIgnoreCase)) ?? ordered.First();
        await ActivateTabAsync(target.Id);
    }


    private void RefreshTabList()
    {
        TabsList.ItemsSource = null;
        TabsList.ItemsSource = sessionTabs.OrderBy(tab => tab.Order).Select(tab => tabs[tab.Id]).ToList();
    }

    private async void AddTemporaryTab_Click(object sender, RoutedEventArgs e)
    {
        if (sessionTabs.Count >= 20) return;
        var definition = new GameProfileTab
        {
            Id = $"temp-{Guid.NewGuid():N}",
            Name = Localization.Get("Ui165"),
            Url = BrowserUrlPolicy.BlankPageUrl,
            Order = sessionTabs.Count
        };
        sessionTabs.Add(definition);
        tabs[definition.Id] = new TabRuntime(definition);
        RefreshTabList();
        await ActivateTabAsync(definition.Id);
        Log.Information("Temporary Companion tab added. Tab={Tab}", definition.Id);
    }

    private async void CloseTab_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not TabRuntime runtime) return;
        await CloseTabAsync(runtime.Definition.Id);
    }

    private async Task CloseTabAsync(string id)
    {
        var index = sessionTabs.FindIndex(tab => string.Equals(tab.Id, id, StringComparison.OrdinalIgnoreCase));
        if (index < 0) return;

        if (tabs.Remove(id, out var runtime))
        {
            try { runtime.View?.Dispose(); } catch (Exception ex) { LogBrowserFailure("close-tab", ex); }
            if (runtime.View is not null) BrowserHost.Children.Remove(runtime.View);
        }
        sessionTabs.RemoveAt(index);
        for (var i = 0; i < sessionTabs.Count; i++) sessionTabs[i] = sessionTabs[i] with { Order = i };
        activeTab = null;

        if (sessionTabs.Count == 0)
        {
            var replacement = new GameProfileTab
            {
                Id = $"temp-{Guid.NewGuid():N}",
                Name = Localization.Get("Ui165"),
                Url = BrowserUrlPolicy.BlankPageUrl,
                Order = 0
            };
            sessionTabs.Add(replacement);
            tabs[replacement.Id] = new TabRuntime(replacement);
        }

        RefreshTabList();
        var nextIndex = Math.Clamp(index - 1, 0, sessionTabs.Count - 1);
        await ActivateTabAsync(sessionTabs[nextIndex].Id);
        Log.Information("Companion tab closed. Tab={Tab}", id);
    }

    private async Task ActivateTabAsync(string id)
    {
        if (isClosing || !tabs.TryGetValue(id, out var runtime)) return;
        await EnsureEnvironmentAsync();
        if (runtime.View is null) await InitializeTabAsync(runtime);
        if (activeTab?.View is not null) activeTab.View.Visibility = Visibility.Collapsed;
        activeTab = runtime;
        runtime.View!.Visibility = Visibility.Visible;
        HideTouchKeyboard();
        viewModel.ReportReady();
        viewModel.ReportNavigation(runtime.CurrentUrl);
        SyncAddressBar(runtime.CurrentUrl);
        UpdateHistory(runtime);
        ClearPageError();
        Log.Information("Companion tab activated. Profile={Profile}; Tab={Tab}; Url={Url}", activeProfile?.DisplayName ?? "manual", runtime.Definition.Name, runtime.CurrentUrl);
    }

    private async Task InitializeTabAsync(TabRuntime runtime)
    {
        if (environment is null) throw new InvalidOperationException("WebView2 environment is not initialized.");
        var view = new WebView2 { Visibility = Visibility.Collapsed };
        runtime.View = view;
        BrowserHost.Children.Add(view);
        await view.EnsureCoreWebView2Async(environment);
        if (view.CoreWebView2 is null) throw new InvalidOperationException("WebView2 Core was not created.");
        runtime.Core = view.CoreWebView2;
        ConfigureBrowser(runtime.Core);
        await InstallKeyboardBridgeAsync(runtime);
        var contentFolder = Path.Combine(AppContext.BaseDirectory, "TouchTestPage");
        runtime.Core.SetVirtualHostNameToFolderMapping("touch-test.local", contentFolder, CoreWebView2HostResourceAccessKind.Deny);
        AttachBrowserEvents(runtime);
        Navigate(runtime, runtime.Definition.Url);
        Log.Information("WebView created lazily for tab. Tab={Tab}; Url={Url}", runtime.Definition.Name, runtime.Definition.Url);
    }

    private static void ConfigureBrowser(CoreWebView2 core)
    {
        var settings = core.Settings;
        settings.AreHostObjectsAllowed = false;
        settings.IsWebMessageEnabled = true;
        settings.IsStatusBarEnabled = false;
        settings.AreDefaultContextMenusEnabled = false;
        settings.AreDefaultScriptDialogsEnabled = false;
        settings.AreDevToolsEnabled = false;
        settings.AreBrowserAcceleratorKeysEnabled = false;
        settings.IsGeneralAutofillEnabled = false;
        settings.IsPasswordAutosaveEnabled = false;
        settings.IsZoomControlEnabled = true;
        settings.IsPinchZoomEnabled = true;
    }

    private void AttachBrowserEvents(TabRuntime tab)
    {
        var core = tab.Core!;
        core.NavigationStarting += (_, e) => NavigationStarting(tab, e);
        core.FrameNavigationStarting += (_, e) => FrameNavigationStarting(tab, e);
        core.NavigationCompleted += (_, e) => NavigationCompleted(tab, e);
        core.SourceChanged += (_, _) => SourceChanged(tab);
        core.HistoryChanged += (_, _) => UpdateHistory(tab);
        core.DocumentTitleChanged += (_, _) => UpdateDocumentTitle(tab);
        core.WebMessageReceived += (_, e) => WebMessageReceived(tab, e);
        core.NewWindowRequested += (_, e) => NewWindowRequested(tab, e);
        core.DownloadStarting += (_, e) => { e.Cancel = true; e.Handled = true; ReportBlocked("download", "Las descargas están deshabilitadas en Companion."); };
        core.PermissionRequested += (_, e) => { e.State = CoreWebView2PermissionState.Deny; e.Handled = true; e.SavesInProfile = false; ReportBlocked("permission", "Se denegó un permiso solicitado por la página."); };
        core.LaunchingExternalUriScheme += (_, e) => { e.Cancel = true; ReportBlocked("external-uri", "Abrir aplicaciones externas está deshabilitado en Companion."); };
        core.BasicAuthenticationRequested += (_, e) => { e.Cancel = true; ReportBlocked("authentication", "Esta página requiere un diálogo de autenticación que Companion no admite."); };
        core.ClientCertificateRequested += (_, e) => { e.Cancel = true; e.Handled = true; ReportBlocked("client-certificate", "Esta página requiere seleccionar un certificado. La solicitud se canceló."); };
        core.ServerCertificateErrorDetected += (_, e) => { e.Action = CoreWebView2ServerCertificateErrorAction.Cancel; ReportBlocked("server-certificate", "Certificado del sitio no válido. No se continuará con una conexión insegura."); };
        core.ScriptDialogOpening += (_, _) => ReportBlocked("script-dialog", "Se cerró un diálogo de la página para mantener la interacción sin ventanas adicionales.");
        core.WindowCloseRequested += (_, _) => ReportBlocked("window-close", "La página solicitó cerrar la ventana. Usa Cerrar para cerrar Companion.");
        core.ProcessFailed += (_, e) => ProcessFailed(tab, e);
    }

    private void DisposeTabs()
    {
        foreach (var runtime in tabs.Values)
        {
            try { runtime.View?.Dispose(); }
            catch (Exception ex) { LogBrowserFailure("dispose-tab", ex); }
        }
        BrowserHost.Children.Clear();
        tabs.Clear();
        sessionTabs.Clear();
        activeTab = null;
    }

    private void NavigateRequested(string url)
    {
        if (activeTab is null || activeTab.Core is null || isClosing) return;
        Navigate(activeTab, url);
    }

    private void Navigate(TabRuntime tab, string url)
    {
        if (tab.RequiresReopen || tab.Core is null) return;
        if (!BrowserUrlPolicy.IsAllowed(url))
        {
            viewModel.ReportError("Navegación bloqueada: solo se permiten direcciones HTTP y HTTPS sin credenciales.");
            return;
        }
        tab.LastNavigationTarget = url;
        RunBrowserCommand(tab, "navigate", () => tab.Core.Navigate(url));
    }

    private void NavigationStarting(TabRuntime tab, CoreWebView2NavigationStartingEventArgs e)
    {
        if (isClosing || !BrowserUrlPolicy.IsAllowed(e.Uri))
        {
            e.Cancel = true;
            if (!isClosing) ReportBlocked("navigation", "Enlace bloqueado: solo se permiten direcciones HTTP y HTTPS sin credenciales.");
            return;
        }
        tab.NavigationId = e.NavigationId;
        tab.LastNavigationTarget = e.Uri;
        if (ReferenceEquals(tab, activeTab)) ClearPageError();
        Log.Information("Browser navigation starting. Tab={Tab}; Redirect={Redirect}", tab.Definition.Name, e.IsRedirected);
    }

    private void FrameNavigationStarting(TabRuntime tab, CoreWebView2NavigationStartingEventArgs e)
    {
        if (!isClosing && (e.Uri is "about:blank" or "about:srcdoc" || BrowserUrlPolicy.IsAllowed(e.Uri))) return;
        e.Cancel = true;
        if (!isClosing) ReportBlocked("frame-navigation", "Se bloqueó contenido incrustado con una dirección no permitida.");
    }

    private void NavigationCompleted(TabRuntime tab, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (isClosing || e.NavigationId != tab.NavigationId) return;
        UpdateHistory(tab);
        UpdateDocumentTitle(tab);
        Log.Information("Browser navigation completed. Tab={Tab}; Success={Success}; Status={Status}; HttpStatus={HttpStatus}", tab.Definition.Name, e.IsSuccess, e.WebErrorStatus, e.HttpStatusCode);
        if (!ReferenceEquals(tab, activeTab)) return;
        if (!e.IsSuccess && e.WebErrorStatus != CoreWebView2WebErrorStatus.OperationCanceled)
            ShowError(LocalizedMessage.Format($"No se pudo cargar la página ({e.WebErrorStatus}). Comprueba la conexión y reintenta, o vuelve a inicio."));
        else if (e.HttpStatusCode >= 400)
            ShowError(LocalizedMessage.Format($"El sitio devolvió HTTP {e.HttpStatusCode}. Reintenta o vuelve a inicio."));
        else ClearPageError();
    }

    private void SourceChanged(TabRuntime tab)
    {
        if (isClosing || tab.Core is null || !BrowserUrlPolicy.IsAllowed(tab.Core.Source)) return;
        tab.CurrentUrl = tab.Core.Source;
        if (string.IsNullOrWhiteSpace(tab.Core.DocumentTitle)) tab.Title = BuildFallbackTabTitle(tab.CurrentUrl);
        if (ReferenceEquals(tab, activeTab))
        {
            viewModel.ReportNavigation(tab.CurrentUrl);
            SyncAddressBar(tab.CurrentUrl);
        }
    }

    private void UpdateHistory(TabRuntime tab)
    {
        if (isClosing || tab.Core is null || !ReferenceEquals(tab, activeTab)) return;
        viewModel.ReportHistory(tab.Core.CanGoBack, tab.Core.CanGoForward);
    }

    private void NewWindowRequested(TabRuntime tab, CoreWebView2NewWindowRequestedEventArgs e)
    {
        e.Handled = true;
        if (isClosing) return;
        if (e.IsUserInitiated && BrowserUrlPolicy.IsAllowed(e.Uri)) Navigate(tab, e.Uri);
        else ReportBlocked("new-window", "Se bloqueó una ventana emergente automática o una dirección no permitida.");
    }

    private void ProcessFailed(TabRuntime tab, CoreWebView2ProcessFailedEventArgs e)
    {
        if (isClosing) return;
        Log.Warning("Browser process failure. Tab={Tab}; Kind={Kind}; Reason={Reason}; Code={Code}", tab.Definition.Name, e.ProcessFailedKind, e.Reason, e.ExitCode);
        if (!ReferenceEquals(tab, activeTab)) return;
        if (e.ProcessFailedKind == CoreWebView2ProcessFailedKind.BrowserProcessExited)
        {
            tab.RequiresReopen = true;
            ShowError("El proceso del navegador se cerró. Cierra Companion y vuelve a abrirlo desde Configuración.");
        }
        else if (e.ProcessFailedKind is CoreWebView2ProcessFailedKind.RenderProcessExited or CoreWebView2ProcessFailedKind.RenderProcessUnresponsive)
            ShowError("La página dejó de responder. Reintenta o vuelve a inicio.");
    }

    private void ReportBlocked(string eventType, string message)
    {
        if (isClosing) return;
        viewModel.ReportError(message);
        Log.Warning("Browser action blocked. Event={Event}; Profile={Profile}; Tab={Tab}", eventType, activeProfile?.DisplayName ?? "manual", activeTab?.Definition.Name ?? "none");
    }

    private nint WindowProc(nint window, int message, nint wParam, nint lParam, ref bool handled)
    {
        if (NoActivateWindowService.TryHandleMessage(message, out var result))
        {
            handled = true;
            Log.Debug("WM_MOUSEACTIVATE -> MA_NOACTIVATE for HWND={Hwnd:X}", window);
            return result;
        }
        if (!isClosing && message == NativeConstants.WmDpiChanged) _ = Dispatcher.InvokeAsync(() => PlaceOnMonitor(targetMonitor));
        return nint.Zero;
    }

    private async void Tab_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is TabRuntime tab) await ActivateTabAsync(tab.Definition.Id);
    }

    private void ToggleProfiles_Click(object sender, RoutedEventArgs e)
    {
        FavoritesPanel.Visibility = Visibility.Collapsed;
        ProfilesPanel.Visibility = ProfilesPanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
    }

    private async void SwitchProfile_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not GameProfile profile || SwitchProfileRequested is null) return;
        ProfilesPanel.Visibility = Visibility.Collapsed;
        try
        {
            Log.Information("Profile switch requested from Companion. Profile={Profile}", profile.DisplayName);
            await SwitchProfileRequested(profile);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Profile switch from Companion failed. Profile={Profile}", profile.DisplayName);
            ShowError("No se pudo cambiar de perfil. Revisa Configuración y los logs.");
        }
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        if (activeTab?.Core is { CanGoBack: true } core) RunBrowserCommand(activeTab, "back", core.GoBack);
    }
    private void Forward_Click(object sender, RoutedEventArgs e)
    {
        if (activeTab?.Core is { CanGoForward: true } core) RunBrowserCommand(activeTab, "forward", core.GoForward);
    }
    private void Reload_Click(object sender, RoutedEventArgs e)
    {
        if (activeTab?.Core is { } core) RunBrowserCommand(activeTab, "reload", core.Reload);
    }
    private void Home_Click(object sender, RoutedEventArgs e) => viewModel.GoHome();
    private async void AddFavorite_Click(object sender, RoutedEventArgs e)
    {
        if (activeTab is not null) await viewModel.AddFavoriteAsync(activeTab.CurrentUrl);
    }
    private async void ToggleToolbar_Click(object sender, RoutedEventArgs e)
    {
        FavoritesPanel.Visibility = Visibility.Collapsed;
        ProfilesPanel.Visibility = Visibility.Collapsed;
        var visible = !viewModel.ShowToolbar;
        await viewModel.SetToolbarVisibleAsync(visible);
        if (!visible) HideTouchKeyboard();
        ApplyAppearance();
    }
    private void ToggleFavorites_Click(object sender, RoutedEventArgs e)
    {
        ProfilesPanel.Visibility = Visibility.Collapsed;
        FavoritesPanel.Visibility = FavoritesPanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
    }
    private void OpenFavorite_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: BrowserFavorite favorite })
        {
            FavoritesPanel.Visibility = Visibility.Collapsed;
            viewModel.Navigate(favorite.Url);
        }
    }
    private async void Retry_Click(object sender, RoutedEventArgs e)
    {
        if (activeTab is null) return;
        if (activeTab.View is null) await ActivateTabAsync(activeTab.Definition.Id);
        else Navigate(activeTab, activeTab.LastNavigationTarget ?? activeTab.CurrentUrl);
    }

    private void RunBrowserCommand(TabRuntime tab, string operation, Action command)
    {
        if (isClosing || tab.Core is null || tab.RequiresReopen) return;
        try { command(); }
        catch (Exception ex)
        {
            LogBrowserFailure(operation, ex);
            if (ReferenceEquals(tab, activeTab)) ShowError("No se pudo completar la navegación. Reintenta o cierra y vuelve a abrir Companion.");
        }
    }

    private static string BuildFallbackTabTitle(string? url)
    {
        if (string.Equals(url, BrowserUrlPolicy.BlankPageUrl, StringComparison.OrdinalIgnoreCase))
            return Localization.Get("Ui165");
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) && !string.IsNullOrWhiteSpace(uri.Host)
            ? uri.Host
            : Localization.Get("Ui165");
    }

    private void UpdateDocumentTitle(TabRuntime tab)
    {
        if (tab.Core is null) return;
        try { tab.Title = tab.Core.DocumentTitle; }
        catch (Exception ex) { LogBrowserFailure("document-title", ex); }
    }

    private static void LogBrowserFailure(string operation, Exception ex) =>
        Log.Warning(ex, "Browser operation failed. Operation={Operation}; Type={Type}; Code={Code}", operation, ex.GetType().Name, ex.HResult);

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
    private void ShowError(LocalizedMessage message)
    {
        if (isClosing) return;
        if (activeTab?.View is not null) activeTab.View.Visibility = Visibility.Collapsed;
        Localization.Text(ErrorText, message.Render);
        ErrorPanel.Visibility = Visibility.Visible;
        viewModel.ReportError(message);
    }
    private void ClearPageError()
    {
        ErrorPanel.Visibility = Visibility.Collapsed;
        if (activeTab?.View is not null) activeTab.View.Visibility = Visibility.Visible;
    }
}
