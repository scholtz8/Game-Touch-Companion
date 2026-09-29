using System.Diagnostics;
using System.IO;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using GameTouchCompanion.Core;

namespace GameTouchCompanion.App;

public partial class MainWindow
{
    private bool configurationReady;

    private void InitializeConfigurationUx()
    {
        GameProfilesPanel.SetMonitors(viewModel.Monitors);
        viewModel.PropertyChanged += ConfigurationStateChanged;
        viewModel.Monitors.CollectionChanged += MonitorsForPreviewChanged;
        profilesViewModel.Profiles.CollectionChanged += ProfilesForGuideChanged;
        Localization.Text(DiagnosticsEnvironment, () => Localization.T(Localization.F($"App: {typeof(MainWindow).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion}\n") +
            Localization.F($"Windows: {Environment.OSVersion.Version} · Proceso: {(Environment.Is64BitProcess ? "x64" : "x86")}")));
        UpdateSetupGuide();
    }

    private bool SetupMonitorsReady => viewModel.CanOpenCompanion && !viewModel.IsSelectionReviewRequired &&
        viewModel.SelectedGameMonitor is not null && viewModel.SelectedCompanionMonitor is not null &&
        !MonitorSelectionService.IdentityEquals(viewModel.SelectedGameMonitor, viewModel.SelectedCompanionMonitor) &&
        viewModel.ValidateCurrentSelection().IsValid;

    private void UpdateSetupGuide()
    {
        Localization.Text(SetupSummary, () => Localization.T(Localization.F($"Pantallas: {(SetupMonitorsReady ? Localization.T("selección preparada") : Localization.T("revisa selección y avisos"))}\n") +
            Localization.F($"Juego: {Localization.MonitorLabel(viewModel.SelectedGameMonitor)}\n") +
            Localization.F($"Companion: {Localization.MonitorLabel(viewModel.SelectedCompanionMonitor)}\n") +
            Localization.F($"Perfiles guardados: {profilesViewModel.Profiles.Count}. Detección: {Localization.T("siempre activa")}.")));
    }

    private void ConfigurationTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!configurationReady || !ReferenceEquals(e.OriginalSource, ConfigurationTabs)) return;
        UpdateSetupGuide();
        if (DiagnosticsTab.IsSelected) RefreshDiagnostics();
    }

    private void ConfigurationStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!configurationReady) return;
        UpdateSetupGuide();
        if (e.PropertyName is nameof(MainWindowViewModel.SelectedGameMonitor) or nameof(MainWindowViewModel.SelectedCompanionMonitor)) DrawMonitorPreview();
    }
    private void MonitorsForPreviewChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (configurationReady) DrawMonitorPreview();
    }
    private void ProfilesForGuideChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        companion?.SetAvailableProfiles(profilesViewModel.Profiles);
        if (configurationReady) UpdateSetupGuide();
    }
    private void MonitorPreview_SizeChanged(object sender, SizeChangedEventArgs e) { if (configurationReady) DrawMonitorPreview(); }
    private void DrawMonitorPreview()
    {
        MonitorPreviewCanvas.Children.Clear();
        var monitors = viewModel.Monitors.ToArray();
        var items = MonitorPreviewLayout.Create(monitors, Math.Max(0, MonitorPreviewCanvas.ActualWidth - 16), 194);
        var legend = new List<string>();
        foreach (var item in items)
        {
            var number = Array.IndexOf(monitors, item.Monitor) + 1;
            var game = viewModel.SelectedGameMonitor is not null && MonitorSelectionService.IdentityEquals(item.Monitor, viewModel.SelectedGameMonitor);
            var touch = viewModel.SelectedCompanionMonitor is not null && MonitorSelectionService.IdentityEquals(item.Monitor, viewModel.SelectedCompanionMonitor);
            var role = Localization.T(game && touch ? "Juego + Companion" : game ? "Juego" : touch ? "Companion" : "Disponible");
            var description = $"{number} · {role} · {Localization.MonitorLabel(item.Monitor)}";
            legend.Add(description);
            var box = new Border
            {
                Width = item.Width, Height = item.Height, BorderThickness = new Thickness(2), BorderBrush = Brushes.White,
                Background = game ? Brushes.MidnightBlue : touch ? Brushes.Teal : Brushes.DimGray,
                ToolTip = description,
                Child = new Viewbox { Stretch = Stretch.Uniform, Margin = new Thickness(6),
                    Child = new TextBlock { Text = $"{number}\n{role}", TextAlignment = TextAlignment.Center, Foreground = Brushes.White } }
            };
            System.Windows.Automation.AutomationProperties.SetName(box, description);
            Canvas.SetLeft(box, item.X + 8); Canvas.SetTop(box, item.Y + 8);
            MonitorPreviewCanvas.Children.Add(box);
        }
        Localization.Text(MonitorPreviewLegend, () => Localization.T(legend.Count == 0 ? Localization.T("Sin pantallas para mostrar.") : string.Join("\n", legend)));
    }
    private void RefreshDiagnostics_Click(object sender, RoutedEventArgs e) => RefreshDiagnostics();
    private void OpenLogs_Click(object sender, RoutedEventArgs e)
    {
        var path = LogPathResolver.Resolve();
        try
        {
            Directory.CreateDirectory(path);
            Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
            Serilog.Log.Information("Logs folder opened. Directory={LogDirectory}", path);
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Could not open logs folder. Directory={LogDirectory}", path);
            AppDialog.Show(Localization.T("No se pudo abrir la carpeta de logs. Revisa los logs manualmente."), "Game Touch Companion", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RefreshDiagnostics()
    {
        try
        {
            // Empty configured names: foreground only; no process enumeration in production.
            var sampledAt = DateTimeOffset.Now;
            var sampledGameMonitor = viewModel.SelectedGameMonitor;
            var sampledCompanionMonitor = viewModel.SelectedCompanionMonitor;
            var foreground = detectionSource.Capture([]).ForegroundWindow;
            var companionHwnd = companion is null ? 0 : new WindowInteropHelper(companion).Handle;
            Localization.Text(DiagnosticsSnapshot, () => Localization.T(Localization.F($"Muestra manual: {sampledAt:yyyy-MM-dd HH:mm:ss zzz}\n") +
                Localization.F($"Foreground HWND: 0x{foreground:X}\nCompanion HWND: 0x{companionHwnd:X}\n") +
                Localization.F($"Companion foreground: {companionHwnd != 0 && companionHwnd == foreground}\n") +
                Localization.F($"Juego: {Localization.MonitorLabel(sampledGameMonitor)}\n") +
                Localization.F($"Companion: {Localization.MonitorLabel(sampledCompanionMonitor)}")));
        }
        catch (Exception ex)
        {
            Localization.Text(DiagnosticsSnapshot, () => Localization.T(Localization.F($"Muestra no disponible ({ex.GetType().Name}). No se cambió la detección.")));
        }
    }
}
