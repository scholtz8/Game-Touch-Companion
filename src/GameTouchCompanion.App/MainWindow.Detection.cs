using System.IO;
using System.Windows;
using System.Windows.Interop;
using GameTouchCompanion.Core;
using GameTouchCompanion.Native;
using Serilog;

namespace GameTouchCompanion.App;

public partial class MainWindow
{
    private readonly IGameDetectionSource detectionSource;
    private readonly GameDetectionTracker detectionTracker = new();
    private CancellationTokenSource? detectionCancellation;
    private Task detectionTask = Task.CompletedTask;
    private bool isClosed;
    private GameProcessInfo? sampledProcess;
    private bool sampledForeground;
    private int sampledLosses;
    private bool captureFailureLogged;
    private string lastDetectionStatusLog = string.Empty;
    private string lastDetectionNoticeLog = string.Empty;
    private bool detectionNoticeHasError;

    private void SetDetectionStatus(string message, bool error = false)
    {
        Localization.Text(DetectionStatus, () => Localization.T(message));
        if (error && !detectionNoticeHasError) ShowDetectionError(message);
        else if (!error && !detectionNoticeHasError) ClearDetectionError();
        var rendered = Localization.T(message);
        if (string.Equals(lastDetectionStatusLog, rendered, StringComparison.Ordinal)) return;
        lastDetectionStatusLog = rendered;
        Log.Debug("Detection status: {Status}", rendered);
    }

    private void SetDetectionNotice(string message, bool warning = false)
    {
        Localization.Text(DetectionNotice, () => Localization.T(message));
        detectionNoticeHasError = warning;
        if (warning) ShowDetectionError(message);
        else ClearDetectionError();
        var rendered = Localization.T(message);
        if (string.Equals(lastDetectionNoticeLog, rendered, StringComparison.Ordinal)) return;
        lastDetectionNoticeLog = rendered;
        if (warning) Log.Warning("Detection notice: {Notice}", rendered);
        else Log.Information("Detection notice: {Notice}", rendered);
    }

    private void ShowDetectionError(string message)
    {
        Localization.Text(DetectionErrorText, () => Localization.T(message));
        DetectionErrorPanel.Visibility = Visibility.Visible;
    }

    private void ClearDetectionError()
    {
        System.Windows.Data.BindingOperations.ClearBinding(DetectionErrorText, System.Windows.Controls.TextBlock.TextProperty);
        DetectionErrorText.Text = string.Empty;
        DetectionErrorPanel.Visibility = Visibility.Collapsed;
    }

    private void DetectionEnabled_Changed(object sender, RoutedEventArgs e)
    {
        tray?.UpdateDetection(DetectionEnabledCheck.IsChecked == true);
        detectionCancellation?.Cancel();
        detectionCancellation?.Dispose();
        detectionCancellation = null;
        if (isClosed || DetectionEnabledCheck.IsChecked != true)
        {
            SetDetectionStatus("Detección pausada. No se abrirá Companion automáticamente.");
            return;
        }
        detectionCancellation = new CancellationTokenSource();
        detectionTask = StartDetectionAfterAsync(detectionTask, detectionCancellation.Token);
    }

    private async Task StartDetectionAfterAsync(Task previousTask, CancellationToken cancellationToken)
    {
        try
        {
            await previousTask;
            cancellationToken.ThrowIfCancellationRequested();
            detectionTracker.ClearObservation();
            sampledProcess = null;
            sampledForeground = false;
            while (!cancellationToken.IsCancellationRequested)
            {
                if (!profilesViewModel.CanEdit)
                    SetDetectionStatus("Esperando perfiles disponibles y sin operaciones pendientes.");
                else
                {
                    var profiles = profilesViewModel.Profiles.ToArray();
                    var names = profiles.Select(p => p.ProcessName).Where(n => n.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                    try
                    {
                        var snapshot = await Task.Run(() => detectionSource.Capture(names), cancellationToken);
                        cancellationToken.ThrowIfCancellationRequested();
                        if (isClosed) return;
                        captureFailureLogged = false;
                        var decision = detectionTracker.Observe(snapshot, profiles);
                        ShowDetectionState(decision, snapshot.ForegroundWindow);
                        if (decision.ShouldLaunch && decision.Game is not null)
                        {
                            detectionTracker.MarkAttempted(decision.Game);
                            try
                            {
                                await OpenDetectedGameAsync(decision.Game, cancellationToken);
                                SetDetectionNotice("Perfil aplicado y Companion abierto sin activación. La instancia no se reabrirá automáticamente.");
                            }
                            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
                            catch (InvalidDataException ex) { SetDetectionNotice(Localization.T(ex.Message) + Localization.T(" Corrige la situación y pulsa Rearmar."), warning: true); }
                            catch (Exception ex)
                            {
                                Log.Warning("Automatic Companion opening failed. Type={Type}; Code={Code}", ex.GetType().Name, ex.HResult);
                                SetDetectionNotice("No se pudo abrir Companion automáticamente. Revisa configuración/runtime y pulsa Rearmar.", warning: true);
                            }
                        }
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
                    catch (Exception ex)
                    {
                        detectionTracker.ClearObservation();
                        sampledProcess = null;
                        sampledForeground = false;
                        if (!captureFailureLogged) Log.Warning("Detection capture failed. Type={Type}; Code={Code}", ex.GetType().Name, ex.HResult);
                        captureFailureLogged = true;
                        Localization.Text(DetectedGameText, () => Localization.T("Muestra no disponible; no se aplicará ningún perfil."));
                        SetDetectionStatus("No se pudo consultar el escritorio. Se intentará de nuevo sin elevar permisos.", error: true);
                    }
                }
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
    }

    private void ShowDetectionState(DetectionDecision decision, nint foreground)
    {
        SetDetectionStatus(decision.Status);
        var game = decision.Game;
        if (game is not null)
        {
            if (sampledProcess == game.Process && sampledForeground && !decision.IsForeground) sampledLosses++;
            if (sampledProcess != game.Process)
                Log.Information("Configured game detected. PID={PID}; HWND={HWND:X}", game.Process.ProcessId, game.Window.Handle);
            Localization.Text(DetectedGameText, () => Localization.T(Localization.F($"{game.Profile.DisplayName} · {game.Process.ProcessName}\nPID: {game.Process.ProcessId} · HWND: 0x{game.Window.Handle:X}\n") +
                Localization.F($"Foreground del juego: {decision.IsForeground} · Bounds: {game.Window.Bounds}")));
        }
        else Localization.Text(DetectedGameText, () => Localization.T("Sin juego detectado. Companion ya abierto se conserva al terminar el juego."));
        sampledProcess = game?.Process;
        sampledForeground = decision.IsForeground;
        var lossesAtSample = sampledLosses;
        var companionHandle = companion is null ? 0 : new WindowInteropHelper(companion).Handle;
        Localization.Text(DetectionDiagnostics, () => Localization.T(Localization.F($"Foreground HWND: 0x{foreground:X} · Companion HWND: 0x{companionHandle:X}\n") +
            Localization.F($"Companion foreground: {companionHandle != 0 && companionHandle == foreground}\n") +
            Localization.F($"Transiciones fuera del juego observadas cada 2 s: {lossesAtSample}. No equivale al contador de FocusProbe.")));
    }

    internal async Task OpenDetectedGameAsync(DetectedGame game, CancellationToken cancellationToken)
    {
        await settingsOperationGate.WaitAsync(cancellationToken);
        ConfigurationTabs.IsEnabled = false;
        try
        {
            EnsureAutomaticCandidate(game, cancellationToken);
            if (!viewModel.CanOpenCompanion || viewModel.IsSelectionReviewRequired)
                throw new InvalidDataException("Revisa y confirma la selección de Pantallas; la detección no puede omitir ese bloqueo.");
            await RefreshMonitorsUnderGateAsync("automatic game detection");
            cancellationToken.ThrowIfCancellationRequested();
            var target = string.IsNullOrWhiteSpace(game.Profile.CompanionMonitor) ? viewModel.SelectedCompanionMonitor :
                viewModel.Monitors.FirstOrDefault(m => string.Equals(m.DeviceName, game.Profile.CompanionMonitor, StringComparison.OrdinalIgnoreCase));
            if (target is null) throw new InvalidDataException("El monitor preferido no está conectado.");
            if (target == viewModel.SelectedGameMonitor || GameDetectionTracker.Intersects(target.Bounds, game.Window.Bounds))
                throw new InvalidDataException("La apertura automática exige una pantalla distinta que no cubra la ventana del juego, incluso con override de pruebas.");
            EnsureAutomaticCandidate(game, cancellationToken);
            await viewModel.ApplyProfileMonitorAsync(target.DeviceName, cancellationToken);
            EnsureAutomaticCandidate(game, cancellationToken);
            // No await or modal UI between final foreground validation and no-activate presentation.
            if (companion is not null) companion.PlaceOnMonitor(target);
            browserViewModel.Navigate(game.Profile.Url);
            ShowCompanionOnSelectedMonitor();
            Log.Information("Automatic profile opening completed. PID={PID}; HWND={HWND:X}", game.Process.ProcessId, game.Window.Handle);
        }
        finally { settingsOperationGate.Release(); if (!isClosed) ConfigurationTabs.IsEnabled = true; }
    }

    private void EnsureAutomaticCandidate(DetectedGame game, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (isClosed) throw new OperationCanceledException(cancellationToken);
        _ = GameProfileValidation.Normalize(game.Profile);
        if (!profilesViewModel.CanEdit || !profilesViewModel.Profiles.Contains(game.Profile) ||
            profilesViewModel.Profiles.Count(p => p.AutoLaunch && string.Equals(p.ProcessName, game.Process.ProcessName, StringComparison.OrdinalIgnoreCase)) != 1)
            throw new InvalidDataException("El perfil cambió o la selección automática es ambigua.");
        if (!detectionSource.IsStillForeground(game))
            throw new InvalidDataException("El juego cambió de ventana, tamaño, instancia o foreground durante la apertura.");
    }

    private void RearmDetection_Click(object sender, RoutedEventArgs e)
    {
        detectionTracker.Reset();
        sampledProcess = null;
        sampledForeground = false;
        sampledLosses = 0;
        SetDetectionNotice("Rearmado. Activa la detección si está pausada y vuelve al juego para dos muestras estables.");
    }

    private void PauseDetection(string reason)
    {
        detectionCancellation?.Cancel();
        DetectionEnabledCheck.IsChecked = false;
        SetDetectionNotice(reason);
    }
}
