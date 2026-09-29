using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;
using GameTouchCompanion.Core;
using Serilog;

namespace GameTouchCompanion.App;

public partial class MainWindow
{
    private readonly IStartupRegistrationService? startupRegistration;
    private readonly ITrayService? tray;
    private bool exitRequested;
    private bool shellInitialized;
    private StartupRegistration? startupState;

    private void InitializeShell()
    {
        if (shellInitialized || isClosed) return;
        shellInitialized = true;
        RefreshStartupRegistration();
        try
        {
            tray?.Initialize(
                () => Dispatcher.Invoke(ShowConfigurationFromTray),
                () => Dispatcher.Invoke(() => { if (!isClosed) RearmDetection_Click(this, new RoutedEventArgs()); }),
                () => Dispatcher.Invoke(ExitCompletely));
            Localization.Text(TrayStatus, () => Localization.T(tray?.IsAvailable == true
                ? Localization.T("Bandeja disponible. Doble clic abre Configuración; el menú permite Rearmar la apertura automática o Salir. Windows decide si el icono queda visible u oculto.")
                : Localization.T("Bandeja no disponible. Configuración no se ocultará; cerrar termina la aplicación.")));
            Log.Debug("Tray availability initialized. Available={Available}", tray?.IsAvailable == true);
        }
        catch (Exception ex)
        {
            Log.Warning("Tray initialization failed. Type={Type}; Code={Code}", ex.GetType().Name, ex.HResult);
            Localization.Text(TrayStatus, () => Localization.T("No se pudo crear el icono de bandeja. La ventana permanecerá accesible."));
            ShowSettingsError(() => Localization.T("No se pudo crear el icono de bandeja. La ventana permanecerá accesible."), "tray-initialization");
        }
    }

    // Production startup initializes before Show, so hidden startup does not flash an activable window.
    internal async Task StartShellAsync(bool launchedFromWindows, bool forceShow = false)
    {
        ShowActivated = !launchedFromWindows;
        new WindowInteropHelper(this).EnsureHandle(); // hidden HWND still receives display-change notifications
        await InitializeConfigurationAsync();
        if (isClosed) return;
        if (!forceShow && viewModel.StartMinimizedToTray && tray?.IsAvailable == true &&
            runtimeAvailable && viewModel.SettingsLoaded && viewModel.CanOpenCompanion && viewModel.ValidateCurrentSelection().IsValid &&
            !viewModel.IsSelectionReviewRequired && browserViewModel.SettingsLoaded && profilesViewModel.CanEdit)
            return;
        Show();
    }

    private void MainWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (!exitRequested && configurationReady && viewModel.CloseToTray && tray?.IsAvailable == true)
        {
            e.Cancel = true;
            Hide();
        }
    }

    internal void ShowConfigurationFromTray()
    {
        if (isClosed) return;
        Show();
        if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
        Activate(); // explicit user request, never called by detection or hot-plug
        RefreshStartupRegistration();
    }

    internal void ExitCompletely()
    {
        if (isClosed) return;
        exitRequested = true;
        Close();
    }
    internal void PrepareForSessionEnd() => exitRequested = true;
    private void ExitCompletely_Click(object sender, RoutedEventArgs e) => ExitCompletely();
    private void HideToTray_Click(object sender, RoutedEventArgs e)
    {
        if (configurationReady && tray?.IsAvailable == true) Hide();
        else
        {
            Localization.Text(TrayStatus, () => Localization.T("Bandeja no disponible. No se ocultó Configuración."));
            ShowSettingsError(() => Localization.T("Bandeja no disponible. No se ocultó Configuración."), "hide-to-tray");
        }
    }

    private void RefreshStartupRegistration()
    {
        WindowsStartupCheck.IsEnabled = startupRegistration is not null;
        RegisterCurrentCopyButton.IsEnabled = startupRegistration is not null;
        try
        {
            var state = startupRegistration?.Read();
            startupState = state;
            WindowsStartupCheck.IsChecked = state?.Exists == true;
            Localization.Text(WindowsStartupStatus, () => Localization.T(state is null ? Localization.T("Registro no disponible en esta sesión de pruebas.") :
                !state.Exists ? Localization.T("Sin registro de inicio de sesión. No se modifica Windows hasta que habilites esta opción.") :
                state.MatchesCurrentExecutable ? Localization.T("Esta copia está registrada. Windows puede retrasar o deshabilitar su inicio desde el Administrador de tareas.") :
                Localization.T("El registro apunta a otra copia/ruta. Desmarca para quitarlo o pulsa Registrar esta copia para reemplazarlo.")));
            Log.Debug("Startup registration refreshed. Available={Available}; Exists={Exists}; MatchesCurrent={MatchesCurrent}", state is not null, state?.Exists == true, state?.MatchesCurrentExecutable == true);
        }
        catch (Exception ex)
        {
            startupState = null;
            WindowsStartupCheck.IsEnabled = false;
            RegisterCurrentCopyButton.IsEnabled = false;
            Localization.Text(WindowsStartupStatus, () => Localization.T("No se pudo consultar el inicio de Windows. No se modificó el registro."));
            Log.Warning("Startup registration read failed. Type={Type}; Code={Code}", ex.GetType().Name, ex.HResult);
            ShowSettingsError(() => Localization.T("No se pudo consultar el inicio de Windows. No se modificó el registro."), "startup-registration-read");
        }
    }
    private void RegisterCurrentCopy_Click(object sender, RoutedEventArgs e) => ChangeStartupRegistration(true, true);
    private void RefreshStartup_Click(object sender, RoutedEventArgs e) => RefreshStartupRegistration();
    private void ChangeStartupRegistration(bool enabled, bool replace)
    {
        if (startupRegistration is null || isClosed) return;
        try
        {
            startupRegistration.SetEnabled(enabled, replace);
            RefreshStartupRegistration();
        }
        catch (Exception ex)
        {
            RefreshStartupRegistration();
            Localization.Text(WindowsStartupStatus, () => Localization.T(Localization.T("No se pudo cambiar el inicio de Windows. Comprueba permisos y que estés usando el EXE desde una ruta estable. ") +
                (ex is ArgumentException ? Localization.Get("StartupPathInvalid") : ex is InvalidOperationException ? Localization.T(ex.Message) : string.Empty)));
            Log.Warning("Startup registration write failed. Type={Type}; Code={Code}", ex.GetType().Name, ex.HResult);
            ShowSettingsError(() => Localization.T(Localization.T("No se pudo cambiar el inicio de Windows. Comprueba permisos y que estés usando el EXE desde una ruta estable. ") +
                (ex is ArgumentException ? Localization.Get("StartupPathInvalid") : ex is InvalidOperationException ? Localization.T(ex.Message) : string.Empty)), "startup-registration-write");
        }
    }
}
