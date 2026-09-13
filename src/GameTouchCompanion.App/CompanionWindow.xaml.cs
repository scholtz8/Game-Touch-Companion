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

public partial class CompanionWindow : Window
{
    private readonly BrowserViewModel viewModel;
    private readonly string userDataFolder;
    private MonitorProfile targetMonitor;
    private nint hwnd;
    private HwndSource? windowSource;
    private CoreWebView2? core;
    private bool isClosing;
    private bool isInitializing;
    private bool requiresReopen;
    private ulong currentNavigationId;
    private string? lastNavigationTarget;

    public CompanionWindow(MonitorProfile targetMonitor, BrowserViewModel viewModel, string? userDataFolder = null)
    {
        this.targetMonitor = targetMonitor ?? throw new ArgumentNullException(nameof(targetMonitor));
        this.viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        this.userDataFolder = userDataFolder ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GameTouchCompanion", "WebView2");
        InitializeComponent();
        DataContext = viewModel;
        InitializeAppearance();
        viewModel.NavigationRequested += NavigateRequested;
        SourceInitialized += OnSourceInitialized;
        Loaded += OnLoaded;
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
        DetachBrowserEvents();
        try { Browser.Dispose(); }
        catch (Exception ex) { LogBrowserFailure("dispose", ex); }
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
        Log.Information("Companion placed without activation. Monitor={Monitor}; Bounds={Bounds}",
            targetMonitor.DeviceName, targetMonitor.Bounds);
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

    private async void OnLoaded(object sender, RoutedEventArgs e) => await InitializeBrowserAsync();

    private async Task InitializeBrowserAsync()
    {
        if (isClosing || isInitializing || requiresReopen || viewModel.IsReady) return;
        isInitializing = true;
        RetryButton.IsEnabled = false;
        try
        {
            _ = CoreWebView2Environment.GetAvailableBrowserVersionString();
            viewModel.ReportStatus("Iniciando navegador…");
            var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: userDataFolder);
            if (isClosing) return;
            await Browser.EnsureCoreWebView2Async(environment);
            if (isClosing || Browser.CoreWebView2 is null) return;
            core = Browser.CoreWebView2;
            ConfigureBrowser();
            var contentFolder = Path.Combine(AppContext.BaseDirectory, "TouchTestPage");
            core.SetVirtualHostNameToFolderMapping("touch-test.local", contentFolder,
                CoreWebView2HostResourceAccessKind.Deny);
            AttachBrowserEvents();
            viewModel.ReportReady();
            NavigateRequested(viewModel.RequestedUrl ?? viewModel.HomeUrl);
            Log.Information("Browser initialized with per-user profile");
        }
        catch (WebView2RuntimeNotFoundException ex) when (!isClosing)
        {
            DetachBrowserEvents();
            LogBrowserFailure("runtime-check", ex);
            ShowError("Falta WebView2 Evergreen Runtime. Instálalo desde Microsoft y vuelve a abrir Companion.");
        }
        catch (Exception ex)
        {
            if (!isClosing)
            {
                DetachBrowserEvents();
                LogBrowserFailure("initialize", ex);
                ShowError("No se pudo iniciar WebView2. Reintenta o cierra y vuelve a abrir Companion. Revisa el Runtime y el acceso al perfil local.");
            }
        }
        finally
        {
            isInitializing = false;
            if (!isClosing) RetryButton.IsEnabled = !requiresReopen;
        }
    }

    private void ConfigureBrowser()
    {
        var settings = core!.Settings;
        settings.AreHostObjectsAllowed = false;
        settings.IsWebMessageEnabled = false;
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

    private void AttachBrowserEvents()
    {
        core!.NavigationStarting += NavigationStarting;
        core.FrameNavigationStarting += FrameNavigationStarting;
        core.NavigationCompleted += NavigationCompleted;
        core.SourceChanged += SourceChanged;
        core.HistoryChanged += HistoryChanged;
        core.NewWindowRequested += NewWindowRequested;
        core.DownloadStarting += DownloadStarting;
        core.PermissionRequested += PermissionRequested;
        core.LaunchingExternalUriScheme += LaunchingExternalUriScheme;
        core.BasicAuthenticationRequested += BasicAuthenticationRequested;
        core.ClientCertificateRequested += ClientCertificateRequested;
        core.ServerCertificateErrorDetected += ServerCertificateErrorDetected;
        core.ScriptDialogOpening += ScriptDialogOpening;
        core.ProcessFailed += ProcessFailed;
        core.WindowCloseRequested += WindowCloseRequested;
    }

    private void DetachBrowserEvents()
    {
        if (core is null) return;
        try
        {
            core.NavigationStarting -= NavigationStarting;
            core.FrameNavigationStarting -= FrameNavigationStarting;
            core.NavigationCompleted -= NavigationCompleted;
            core.SourceChanged -= SourceChanged;
            core.HistoryChanged -= HistoryChanged;
            core.NewWindowRequested -= NewWindowRequested;
            core.DownloadStarting -= DownloadStarting;
            core.PermissionRequested -= PermissionRequested;
            core.LaunchingExternalUriScheme -= LaunchingExternalUriScheme;
            core.BasicAuthenticationRequested -= BasicAuthenticationRequested;
            core.ClientCertificateRequested -= ClientCertificateRequested;
            core.ServerCertificateErrorDetected -= ServerCertificateErrorDetected;
            core.ScriptDialogOpening -= ScriptDialogOpening;
            core.ProcessFailed -= ProcessFailed;
            core.WindowCloseRequested -= WindowCloseRequested;
        }
        catch (Exception ex) { LogBrowserFailure("detach-events", ex); }
        core = null;
    }

    private void NavigateRequested(string url)
    {
        if (isClosing) return;
        if (requiresReopen)
        {
            ShowError("El proceso del navegador se cerró. Cierra Companion y vuelve a abrirlo desde Configuración.");
            return;
        }
        if (!viewModel.IsReady || core is null) return; // RequestedUrl retains the latest queued address.
        if (!BrowserUrlPolicy.IsAllowed(url))
        {
            viewModel.ReportError("Navegación bloqueada: solo se permiten direcciones HTTP y HTTPS sin credenciales.");
            return;
        }
        lastNavigationTarget = url;
        RunBrowserCommand("navigate", () => core.Navigate(url));
    }

    private void NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        if (isClosing || !BrowserUrlPolicy.IsAllowed(e.Uri))
        {
            e.Cancel = true;
            if (!isClosing) ReportBlocked("navigation", "Enlace bloqueado: solo se permiten direcciones HTTP y HTTPS sin credenciales.");
            return;
        }
        currentNavigationId = e.NavigationId;
        lastNavigationTarget = e.Uri;
        ClearPageError();
        viewModel.ReportStatus("Cargando página…");
        Log.Information("Browser navigation starting. Redirect={Redirect}", e.IsRedirected);
    }

    private void FrameNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        // Empty frames are used by ordinary sites; their subsequent navigations are checked again.
        if (!isClosing && (e.Uri is "about:blank" or "about:srcdoc" || BrowserUrlPolicy.IsAllowed(e.Uri))) return;
        e.Cancel = true;
        if (!isClosing) ReportBlocked("frame-navigation", "Se bloqueó contenido incrustado con una dirección no permitida.");
    }

    private void NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (isClosing || e.NavigationId != currentNavigationId) return;
        UpdateHistory();
        Log.Information("Browser navigation completed. Success={Success}; Status={Status}; HttpStatus={HttpStatus}",
            e.IsSuccess, e.WebErrorStatus, e.HttpStatusCode);
        if (!e.IsSuccess)
        {
            if (e.WebErrorStatus == CoreWebView2WebErrorStatus.OperationCanceled) return;
            ShowError(LocalizedMessage.Format($"No se pudo cargar la página ({e.WebErrorStatus}). Comprueba la conexión y reintenta, o vuelve a inicio."));
        }
        else if (e.HttpStatusCode >= 400)
            ShowError(LocalizedMessage.Format($"El sitio devolvió HTTP {e.HttpStatusCode}. Reintenta o vuelve a inicio."));
        else
        {
            ClearPageError();
            viewModel.ReportStatus("Página lista. Las direcciones se editan desde Configuración.");
        }
    }

    private void SourceChanged(object? sender, CoreWebView2SourceChangedEventArgs e)
    {
        if (!isClosing && core is not null) viewModel.ReportNavigation(core.Source);
    }

    private void HistoryChanged(object? sender, object e) => UpdateHistory();

    private void UpdateHistory()
    {
        if (!isClosing && core is not null) viewModel.ReportHistory(core.CanGoBack, core.CanGoForward);
    }

    private void NewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
    {
        e.Handled = true;
        if (isClosing) return;
        if (e.IsUserInitiated && BrowserUrlPolicy.IsAllowed(e.Uri))
        {
            var url = e.Uri;
            _ = Dispatcher.InvokeAsync(() => { if (!isClosing) viewModel.Navigate(url); });
            Log.Information("Browser user-requested new window redirected into Companion");
        }
        else ReportBlocked("new-window", "Se bloqueó una ventana emergente automática o una dirección no permitida.");
    }

    private void DownloadStarting(object? sender, CoreWebView2DownloadStartingEventArgs e)
    {
        e.Cancel = true;
        e.Handled = true;
        ReportBlocked("download", "Las descargas están deshabilitadas en Companion.");
    }

    private void PermissionRequested(object? sender, CoreWebView2PermissionRequestedEventArgs e)
    {
        e.State = CoreWebView2PermissionState.Deny;
        e.Handled = true;
        e.SavesInProfile = false;
        ReportBlocked("permission", "Se denegó un permiso solicitado por la página.");
    }

    private void LaunchingExternalUriScheme(object? sender, CoreWebView2LaunchingExternalUriSchemeEventArgs e)
    {
        e.Cancel = true;
        ReportBlocked("external-uri", "Abrir aplicaciones externas está deshabilitado en Companion.");
    }

    private void BasicAuthenticationRequested(object? sender, CoreWebView2BasicAuthenticationRequestedEventArgs e)
    {
        e.Cancel = true;
        ReportBlocked("authentication", "Esta página requiere un diálogo de autenticación que Companion no admite.");
    }

    private void ClientCertificateRequested(object? sender, CoreWebView2ClientCertificateRequestedEventArgs e)
    {
        e.Cancel = true;
        e.Handled = true;
        ReportBlocked("client-certificate", "Esta página requiere seleccionar un certificado. La solicitud se canceló.");
    }

    private void ServerCertificateErrorDetected(object? sender, CoreWebView2ServerCertificateErrorDetectedEventArgs e)
    {
        e.Action = CoreWebView2ServerCertificateErrorAction.Cancel;
        ReportBlocked("server-certificate", "Certificado del sitio no válido. No se continuará con una conexión insegura.");
    }

    private void ScriptDialogOpening(object? sender, CoreWebView2ScriptDialogOpeningEventArgs e) =>
        ReportBlocked("script-dialog", "Se cerró un diálogo de la página para mantener la interacción sin ventanas adicionales.");

    private void WindowCloseRequested(object? sender, object e) =>
        ReportBlocked("window-close", "La página solicitó cerrar la ventana. Usa Cerrar para cerrar Companion.");

    private void ProcessFailed(object? sender, CoreWebView2ProcessFailedEventArgs e)
    {
        if (isClosing) return;
        Log.Warning("Browser process failure. Kind={Kind}; Reason={Reason}; Code={Code}", e.ProcessFailedKind, e.Reason, e.ExitCode);
        if (e.ProcessFailedKind == CoreWebView2ProcessFailedKind.BrowserProcessExited)
        {
            requiresReopen = true;
            viewModel.ReportClosed();
            RetryButton.IsEnabled = false;
            ShowError("El proceso del navegador se cerró. Cierra Companion y vuelve a abrirlo desde Configuración.");
        }
        else if (e.ProcessFailedKind is CoreWebView2ProcessFailedKind.RenderProcessExited or CoreWebView2ProcessFailedKind.RenderProcessUnresponsive)
            ShowError("La página dejó de responder. Reintenta o vuelve a inicio.");
    }

    private void ReportBlocked(string eventType, string message)
    {
        if (isClosing) return;
        viewModel.ReportError(message);
        Log.Information("Browser action blocked. Event={Event}", eventType);
    }

    private nint WindowProc(nint window, int message, nint wParam, nint lParam, ref bool handled)
    {
        if (NoActivateWindowService.TryHandleMessage(message, out var result))
        {
            handled = true;
            Log.Debug("WM_MOUSEACTIVATE -> MA_NOACTIVATE for HWND={Hwnd:X}", window);
            return result;
        }

        if (!isClosing && message == NativeConstants.WmDpiChanged)
        {
            _ = Dispatcher.InvokeAsync(() => PlaceOnMonitor(targetMonitor));
        }
        return nint.Zero;
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        if (viewModel.CanGoBack) RunBrowserCommand("back", () => core!.GoBack());
    }

    private void Forward_Click(object sender, RoutedEventArgs e)
    {
        if (viewModel.CanGoForward) RunBrowserCommand("forward", () => core!.GoForward());
    }

    private void Reload_Click(object sender, RoutedEventArgs e) => RunBrowserCommand("reload", () => core!.Reload());
    private void Home_Click(object sender, RoutedEventArgs e) => viewModel.GoHome();
    private async void AddFavorite_Click(object sender, RoutedEventArgs e) => await viewModel.AddFavoriteAsync();

    private async void ToggleToolbar_Click(object sender, RoutedEventArgs e)
    {
        FavoritesPanel.Visibility = Visibility.Collapsed;
        await viewModel.SetToolbarVisibleAsync(!viewModel.ShowToolbar);
        ApplyAppearance();
    }

    private void ToggleFavorites_Click(object sender, RoutedEventArgs e) =>
        FavoritesPanel.Visibility = FavoritesPanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;

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
        if (!viewModel.IsReady) await InitializeBrowserAsync();
        else NavigateRequested(lastNavigationTarget ?? viewModel.RequestedUrl ?? viewModel.HomeUrl);
    }

    private void RunBrowserCommand(string operation, Action command)
    {
        if (isClosing || !viewModel.IsReady || core is null || requiresReopen) return;
        try { command(); }
        catch (Exception ex)
        {
            LogBrowserFailure(operation, ex);
            ShowError("No se pudo completar la navegación. Reintenta o cierra y vuelve a abrir Companion.");
        }
    }

    private static void LogBrowserFailure(string operation, Exception ex) =>
        Log.Warning("Browser operation failed. Operation={Operation}; Type={Type}; Code={Code}", operation, ex.GetType().Name, ex.HResult);

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void ShowError(LocalizedMessage message)
    {
        if (isClosing) return;
        Browser.Visibility = Visibility.Collapsed;
        Localization.Text(ErrorText, message.Render);
        ErrorPanel.Visibility = Visibility.Visible;
        viewModel.ReportError(message);
    }

    private void ClearPageError()
    {
        ErrorPanel.Visibility = Visibility.Collapsed;
        Browser.Visibility = Visibility.Visible;
    }
}
