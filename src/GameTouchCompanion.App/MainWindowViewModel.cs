using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using GameTouchCompanion.Core;
using GameTouchCompanion.Native;
using Serilog;

namespace GameTouchCompanion.App;

public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly IMonitorService monitorService;
    private readonly IApplicationSettingsStore settingsStore;
    private readonly MonitorSelectionService selectionService;
    private MonitorProfile? selectedGameMonitor;
    private MonitorProfile? selectedCompanionMonitor;
    private bool allowSameMonitorForTesting;
    private bool canOpenCompanion;
    private bool enableDetectionOnStartup;
    private bool settingsLoaded;
    private bool startMinimizedToTray;
    private bool closeToTray;
    public bool StartMinimizedToTray { get => startMinimizedToTray; private set => SetField(ref startMinimizedToTray, value); }
    public bool CloseToTray { get => closeToTray; private set => SetField(ref closeToTray, value); }
    public bool EnableDetectionOnStartup { get => enableDetectionOnStartup; private set => SetField(ref enableDetectionOnStartup, value); }
    public bool SettingsLoaded { get => settingsLoaded; private set => SetField(ref settingsLoaded, value); }
    private LocalizedMessage monitorStatus = "Detecting monitors…";
    private LocalizedMessage monitorError = string.Empty;
    private bool isSelectionReviewRequired;
    private LocalizedMessage selectionReviewMessage = string.Empty;

    public MainWindowViewModel(
        IMonitorService monitorService,
        IApplicationSettingsStore settingsStore,
        MonitorSelectionService selectionService)
    {
        this.monitorService = monitorService;
        this.settingsStore = settingsStore;
        this.selectionService = selectionService;
    }

    public ObservableCollection<MonitorProfile> Monitors { get; } = [];

    public MonitorProfile? SelectedGameMonitor
    {
        get => selectedGameMonitor;
        set => SetField(ref selectedGameMonitor, value);
    }

    public MonitorProfile? SelectedCompanionMonitor
    {
        get => selectedCompanionMonitor;
        set => SetField(ref selectedCompanionMonitor, value);
    }

    public bool AllowSameMonitorForTesting
    {
        get => allowSameMonitorForTesting;
        set => SetField(ref allowSameMonitorForTesting, value);
    }

    public bool CanOpenCompanion
    {
        get => canOpenCompanion;
        private set => SetField(ref canOpenCompanion, value);
    }

    public string MonitorStatus => monitorStatus.Render();
    private void SetMonitorStatus(LocalizedMessage message)
    {
        monitorStatus = message;
        Log.Debug("Monitor status: {Status}", message.Render());
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MonitorStatus)));
    }


    public string MonitorError => monitorError.Render();
    public bool HasMonitorError => MonitorError.Length > 0;
    private void SetMonitorError(LocalizedMessage message)
    {
        monitorError = message;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MonitorError)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasMonitorError)));
    }

    public bool IsSelectionReviewRequired
    {
        get => isSelectionReviewRequired;
        private set => SetField(ref isSelectionReviewRequired, value);
    }

    public string SelectionReviewMessage => selectionReviewMessage.Render();
    private void SetSelectionReviewMessage(LocalizedMessage message)
    {
        selectionReviewMessage = message;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectionReviewMessage)));
    }


    public bool IsUpdating { get; private set; }

    public void RefreshLanguage()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MonitorStatus)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MonitorError)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasMonitorError)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectionReviewMessage)));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var settings = await settingsStore.LoadAsync(cancellationToken);
        EnableDetectionOnStartup = settings.EnableDetectionOnStartup;
        StartMinimizedToTray = settings.StartMinimizedToTray;
        CloseToTray = settings.CloseToTray;
        await RefreshAsync(settings, cancellationToken);
        SettingsLoaded = true;
    }

    public async Task SetDetectionOnStartupAsync(bool enabled, CancellationToken cancellationToken = default)
    {
        if (!SettingsLoaded) throw new InvalidDataException("La configuración no se cargó correctamente; no se sobrescribirá settings.json.");
        await settingsStore.SaveAsync(CreateSettings() with { EnableDetectionOnStartup = enabled }, cancellationToken);
        EnableDetectionOnStartup = enabled;
    }

    public async Task SetTrayPreferencesAsync(bool startHidden, bool closeHidden, CancellationToken cancellationToken = default)
    {
        if (!SettingsLoaded) throw new InvalidDataException("La configuración no se cargó; no se sobrescribirá settings.json.");
        await settingsStore.SaveAsync(CreateSettings() with { StartMinimizedToTray = startHidden, CloseToTray = closeHidden }, cancellationToken);
        StartMinimizedToTray = startHidden;
        CloseToTray = closeHidden;
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var desiredSettings = CreateSettings();
        await RefreshAsync(desiredSettings, cancellationToken);
    }

    public async Task ApplySelectionAsync(CancellationToken cancellationToken = default)
    {
        if (IsUpdating || SelectedGameMonitor is null || SelectedCompanionMonitor is null)
            return;

        await ApplyResolvedSelectionAsync(CreateSettings(), cancellationToken);
    }

    public MonitorSelectionValidation ValidateCurrentSelection()
    {
        if (SelectedGameMonitor is null || SelectedCompanionMonitor is null)
            return new MonitorSelectionValidation(false,
            [
                new MonitorSelectionIssue(
                    MonitorSelectionIssueCode.CompanionMonitorUnavailable,
                    MonitorSelectionIssueSeverity.Error,
                    "Select both a game monitor and a Companion monitor."),
            ]);

        return selectionService.ValidateSelection(
            SelectedGameMonitor,
            SelectedCompanionMonitor,
            Monitors,
            AllowSameMonitorForTesting);
    }

    public async Task ApplyProfileMonitorAsync(string? deviceName, CancellationToken cancellationToken = default)
    {
        if (IsSelectionReviewRequired || !CanOpenCompanion || SelectedGameMonitor is null || SelectedCompanionMonitor is null)
            throw new InvalidDataException("Revisa y confirma la selección en Pantallas antes de aplicar un perfil.");
        var target = string.IsNullOrWhiteSpace(deviceName) ? SelectedCompanionMonitor :
            Monitors.FirstOrDefault(m => string.Equals(m.DeviceName, deviceName, StringComparison.OrdinalIgnoreCase));
        if (target is null)
            throw new InvalidDataException("El monitor del perfil no está disponible. Reconéctalo o edita la preferencia; no se aplicó ningún cambio.");
        var validation = selectionService.ValidateSelection(SelectedGameMonitor, target, Monitors, AllowSameMonitorForTesting);
        if (!validation.IsValid)
            throw new InvalidDataException("La selección del perfil no es válida. Revisa Pantallas y el permiso de mismo monitor para pruebas.");
        await settingsStore.SaveAsync(CreateSettings() with { CompanionMonitorDeviceName = target.DeviceName }, cancellationToken);
        IsUpdating = true;
        try { SelectedCompanionMonitor = target; SetMonitorError(string.Empty); SetMonitorStatus("Preferencia de monitor del perfil aplicada."); }
        finally { IsUpdating = false; }
    }

    public void ReportStatus(LocalizedMessage message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message.Render());
        SetMonitorError(string.Empty);
        SetMonitorStatus(message);
    }

    public void ReportError(LocalizedMessage message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message.Render());
        Log.Warning("Monitor error: {Error}", message.Render());
        SetMonitorError(message);
        SetMonitorStatus(message);
        CanOpenCompanion = false;
    }

    public void RequireSelectionReview(LocalizedMessage message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message.Render());
        SetSelectionReviewMessage(message);
        IsSelectionReviewRequired = true;
        CanOpenCompanion = false;
    }

    public bool ConfirmSelectionReview()
    {
        var validation = ValidateCurrentSelection();
        if (!validation.IsValid)
        {
            var message = new LocalizedMessage(() => string.Join(" ", validation.Issues.Select(static issue => Localization.MonitorIssue(issue))));
            SetMonitorError(message);
            SetMonitorStatus(message);
            CanOpenCompanion = false;
            return false;
        }

        IsSelectionReviewRequired = false;
        SetSelectionReviewMessage(string.Empty);
        CanOpenCompanion = true;
        SetMonitorError(string.Empty);
        SetMonitorStatus(LocalizedMessage.Format($"Selection confirmed. Companion: {SelectedCompanionMonitor!.DeviceName}."));
        return true;
    }

    private async Task RefreshAsync(ApplicationSettings desiredSettings, CancellationToken cancellationToken)
    {
        var detectedMonitors = monitorService.GetMonitors();
        if (detectedMonitors.Count == 0)
        {
            IsUpdating = true;
            try
            {
                Monitors.Clear();
                SelectedGameMonitor = null;
                SelectedCompanionMonitor = null;
                CanOpenCompanion = false;
                SetMonitorError("No monitors were detected. Refresh after reconnecting a display.");
                SetMonitorStatus("No monitors were detected. Refresh after reconnecting a display.");
            }
            finally
            {
                IsUpdating = false;
            }
            return;
        }

        IsUpdating = true;
        try
        {
            Monitors.Clear();
            foreach (var monitor in detectedMonitors)
                Monitors.Add(monitor);
        }
        finally
        {
            IsUpdating = false;
        }

        await ApplyResolvedSelectionAsync(desiredSettings, cancellationToken);
    }

    private async Task ApplyResolvedSelectionAsync(ApplicationSettings desiredSettings, CancellationToken cancellationToken)
    {
        var resolved = selectionService.Resolve(Monitors, desiredSettings);

        IsUpdating = true;
        try
        {
            SelectedGameMonitor = resolved.GameMonitor;
            SelectedCompanionMonitor = resolved.CompanionMonitor;
            AllowSameMonitorForTesting = desiredSettings.AllowSameMonitorForTesting;
            CanOpenCompanion = !IsSelectionReviewRequired;
            SetMonitorError(string.Empty);
            SetMonitorStatus(BuildStatus(resolved));
        }
        finally
        {
            IsUpdating = false;
        }

        var normalizedSettings = new ApplicationSettings
        {
            GameMonitorDeviceName = resolved.GameMonitor.DeviceName,
            CompanionMonitorDeviceName = resolved.CompanionMonitor.DeviceName,
            AllowSameMonitorForTesting = AllowSameMonitorForTesting,
            EnableDetectionOnStartup = EnableDetectionOnStartup,
            StartMinimizedToTray = StartMinimizedToTray,
            CloseToTray = CloseToTray,
        };
        await settingsStore.SaveAsync(normalizedSettings, cancellationToken);
    }

    private ApplicationSettings CreateSettings() => new()
    {
        GameMonitorDeviceName = SelectedGameMonitor?.DeviceName,
        CompanionMonitorDeviceName = SelectedCompanionMonitor?.DeviceName,
        AllowSameMonitorForTesting = AllowSameMonitorForTesting,
        EnableDetectionOnStartup = EnableDetectionOnStartup,
        StartMinimizedToTray = StartMinimizedToTray,
        CloseToTray = CloseToTray,
    };

    private LocalizedMessage BuildStatus(ResolvedMonitorSelection selection)
    {
        return new LocalizedMessage(() =>
        {
            var prefix = Localization.F($"{Monitors.Count} monitor(s) detected. Companion: {selection.CompanionMonitor.DeviceName}.");
            return selection.Issues.Count == 0 ? prefix : prefix + " " + string.Join(" ", selection.Issues.Select(issue => Localization.MonitorIssue(issue)));
        });
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
