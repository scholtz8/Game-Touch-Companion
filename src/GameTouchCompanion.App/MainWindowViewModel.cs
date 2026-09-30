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
    private ApplicationSettings persistedSettings = new();
    private MonitorProfile? selectedGameMonitor;
    private MonitorProfile? selectedCompanionMonitor;
    private bool allowSameMonitorForTesting;
    private bool canOpenCompanion;
    private bool settingsLoaded;
    private bool startMinimizedToTray;
    private bool closeToTray;
    private bool legacyMonitorSelectionRequiresReview;
    private LocalizedMessage monitorStatus = "Detecting monitors…";
    private LocalizedMessage monitorError = string.Empty;
    private bool isSelectionReviewRequired;
    private LocalizedMessage selectionReviewMessage = string.Empty;

    public MainWindowViewModel(IMonitorService monitorService, IApplicationSettingsStore settingsStore, MonitorSelectionService selectionService)
    {
        this.monitorService = monitorService;
        this.settingsStore = settingsStore;
        this.selectionService = selectionService;
    }

    public ObservableCollection<MonitorProfile> Monitors { get; } = [];
    public MonitorProfile? SelectedGameMonitor { get => selectedGameMonitor; set => SetField(ref selectedGameMonitor, value); }
    public MonitorProfile? SelectedCompanionMonitor { get => selectedCompanionMonitor; set => SetField(ref selectedCompanionMonitor, value); }
    public bool AllowSameMonitorForTesting { get => allowSameMonitorForTesting; set => SetField(ref allowSameMonitorForTesting, value); }
    public bool CanOpenCompanion { get => canOpenCompanion; private set => SetField(ref canOpenCompanion, value); }
    public bool StartMinimizedToTray { get => startMinimizedToTray; private set => SetField(ref startMinimizedToTray, value); }
    public bool CloseToTray { get => closeToTray; private set => SetField(ref closeToTray, value); }
    public bool SettingsLoaded { get => settingsLoaded; private set => SetField(ref settingsLoaded, value); }
    public bool IsUpdating { get; private set; }

    public string MonitorStatus => monitorStatus.Render();
    public string MonitorError => monitorError.Render();
    public bool HasMonitorError => MonitorError.Length > 0;
    public bool IsSelectionReviewRequired { get => isSelectionReviewRequired; private set => SetField(ref isSelectionReviewRequired, value); }
    public string SelectionReviewMessage => selectionReviewMessage.Render();

    public event PropertyChangedEventHandler? PropertyChanged;

    public void RefreshLanguage()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MonitorStatus)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MonitorError)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasMonitorError)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectionReviewMessage)));
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var settings = await settingsStore.LoadAsync(cancellationToken);
        persistedSettings = settings;
        legacyMonitorSelectionRequiresReview = HasLegacyMonitorReference(settings.GameMonitorId, settings.GameMonitorDeviceName) ||
            HasLegacyMonitorReference(settings.CompanionMonitorId, settings.CompanionMonitorDeviceName);
        StartMinimizedToTray = settings.StartMinimizedToTray;
        CloseToTray = settings.CloseToTray;
        await RefreshAsync(settings, cancellationToken);
        SettingsLoaded = true;
    }

    public async Task SetTrayPreferencesAsync(bool startHidden, bool closeHidden, CancellationToken cancellationToken = default)
    {
        if (!SettingsLoaded) throw new InvalidDataException("La configuración no se cargó; no se sobrescribirá settings.json.");
        var next = persistedSettings with { StartMinimizedToTray = startHidden, CloseToTray = closeHidden };
        await settingsStore.SaveAsync(next, cancellationToken);
        persistedSettings = next;
        StartMinimizedToTray = startHidden;
        CloseToTray = closeHidden;
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default) =>
        await RefreshAsync(persistedSettings, cancellationToken);

    public async Task ApplySelectionAsync(CancellationToken cancellationToken = default)
    {
        if (IsUpdating || SelectedGameMonitor is null || SelectedCompanionMonitor is null) return;

        var desired = persistedSettings with
        {
            GameMonitorId = SelectedGameMonitor.StableId,
            CompanionMonitorId = SelectedCompanionMonitor.StableId,
            GameMonitorDeviceName = SelectedGameMonitor.DeviceName,
            CompanionMonitorDeviceName = SelectedCompanionMonitor.DeviceName,
            AllowSameMonitorForTesting = AllowSameMonitorForTesting,
            StartMinimizedToTray = StartMinimizedToTray,
            CloseToTray = CloseToTray,
        };
        await ApplyResolvedSelectionAsync(desired, cancellationToken, explicitUserSelection: true);
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

        return selectionService.ValidateSelection(SelectedGameMonitor, SelectedCompanionMonitor, Monitors, AllowSameMonitorForTesting);
    }

    public MonitorProfile? ResolveMonitorReference(string? reference) => MonitorSelectionService.FindByReference(Monitors, reference);

    public async Task ApplyProfileMonitorAsync(string? monitorReference, CancellationToken cancellationToken = default)
    {
        if (IsSelectionReviewRequired || !CanOpenCompanion || SelectedGameMonitor is null || SelectedCompanionMonitor is null)
            throw new InvalidDataException("Revisa y confirma la selección en Pantallas antes de aplicar un perfil.");

        var target = string.IsNullOrWhiteSpace(monitorReference) ? SelectedCompanionMonitor : ResolveMonitorReference(monitorReference);
        if (target is null)
            throw new InvalidDataException("El monitor del perfil no está disponible. Reconéctalo o edita la preferencia; no se aplicó ningún cambio.");

        var validation = selectionService.ValidateSelection(SelectedGameMonitor, target, Monitors, AllowSameMonitorForTesting);
        if (!validation.IsValid)
            throw new InvalidDataException("La selección del perfil no es válida. Revisa Pantallas y el permiso de mismo monitor para pruebas.");

        var next = persistedSettings with
        {
            CompanionMonitorId = target.StableId,
            CompanionMonitorDeviceName = target.DeviceName,
            GameMonitorId = SelectedGameMonitor.StableId,
            GameMonitorDeviceName = SelectedGameMonitor.DeviceName,
            AllowSameMonitorForTesting = AllowSameMonitorForTesting,
            StartMinimizedToTray = StartMinimizedToTray,
            CloseToTray = CloseToTray,
        };
        await settingsStore.SaveAsync(next, cancellationToken);
        persistedSettings = next;

        IsUpdating = true;
        try
        {
            SelectedCompanionMonitor = target;
            SetMonitorError(string.Empty);
            SetMonitorStatus("Preferencia de monitor del perfil aplicada.");
        }
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
        // A disconnected persistent identity is not a generic fallback warning. The user must
        // either reconnect that physical display or explicitly select another connected display,
        // which persists a new identity through ApplySelectionAsync. A plain confirmation must
        // never authorize whichever DISPLAYn currently occupies the old slot.
        if (IsPersistentIdentityMissing(persistedSettings.GameMonitorId) ||
            IsPersistentIdentityMissing(persistedSettings.CompanionMonitorId))
        {
            CanOpenCompanion = false;
            return false;
        }

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
        SetMonitorStatus(LocalizedMessage.Format($"Selection confirmed. Companion: {Localization.MonitorLabel(SelectedCompanionMonitor)}."));
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
            finally { IsUpdating = false; }
            return;
        }

        IsUpdating = true;
        try
        {
            Monitors.Clear();
            foreach (var monitor in detectedMonitors) Monitors.Add(monitor);
        }
        finally { IsUpdating = false; }

        await ApplyResolvedSelectionAsync(desiredSettings, cancellationToken);
    }

    private async Task ApplyResolvedSelectionAsync(
        ApplicationSettings desiredSettings,
        CancellationToken cancellationToken,
        bool explicitUserSelection = false)
    {
        var resolved = selectionService.Resolve(Monitors, desiredSettings);
        var missingGameIdentity = IsPersistentIdentityMissing(desiredSettings.GameMonitorId);
        var missingCompanionIdentity = IsPersistentIdentityMissing(desiredSettings.CompanionMonitorId);
        var legacySelectionNeedsReview = legacyMonitorSelectionRequiresReview && !explicitUserSelection;

        IsUpdating = true;
        try
        {
            SelectedGameMonitor = resolved.GameMonitor;
            SelectedCompanionMonitor = resolved.CompanionMonitor;
            AllowSameMonitorForTesting = desiredSettings.AllowSameMonitorForTesting;
            SetMonitorError(string.Empty);
            SetMonitorStatus(BuildStatus(resolved));

            if (missingGameIdentity || missingCompanionIdentity)
            {
                IsSelectionReviewRequired = true;
                SetSelectionReviewMessage(missingCompanionIdentity
                    ? "La pantalla Companion configurada no está disponible. Se conserva su ID persistente; reconéctala o elige otra pantalla explícitamente."
                    : "La pantalla de juego configurada no está disponible. Se conserva su ID persistente; reconéctala o elige otra pantalla explícitamente.");
            }
            else if (legacySelectionNeedsReview)
            {
                // Preserve a more specific topology-loss notice that may already be active.
                if (!IsSelectionReviewRequired)
                {
                    IsSelectionReviewRequired = true;
                    SetSelectionReviewMessage("Se encontró una selección antigua basada en DISPLAY1/2/3. Verifica los nombres físicos mostrados y confirma la selección una vez para migrarla al identificador persistente.");
                }
            }
            else if (IsSelectionReviewRequired && (explicitUserSelection || PersistentSelectionMatchesExactly(desiredSettings, resolved)))
            {
                // A topology-loss review can clear itself only when the exact persisted physical
                // identities are present again. This is deliberately stricter than accepting a
                // fallback DISPLAYn and makes reconnecting the configured monitor seamless.
                IsSelectionReviewRequired = false;
                SetSelectionReviewMessage(string.Empty);
            }
            CanOpenCompanion = !IsSelectionReviewRequired;
        }
        finally { IsUpdating = false; }

        // Never replace a missing persistent monitor id with whichever DISPLAYn happens to be present.
        if (missingGameIdentity || missingCompanionIdentity || legacySelectionNeedsReview)
        {
            persistedSettings = desiredSettings;
            return;
        }

        var normalizedSettings = desiredSettings with
        {
            GameMonitorId = resolved.GameMonitor.StableId,
            CompanionMonitorId = resolved.CompanionMonitor.StableId,
            GameMonitorDeviceName = resolved.GameMonitor.DeviceName,
            CompanionMonitorDeviceName = resolved.CompanionMonitor.DeviceName,
            AllowSameMonitorForTesting = AllowSameMonitorForTesting,
            StartMinimizedToTray = StartMinimizedToTray,
            CloseToTray = CloseToTray,
        };
        await settingsStore.SaveAsync(normalizedSettings, cancellationToken);
        persistedSettings = normalizedSettings;
        if (explicitUserSelection) legacyMonitorSelectionRequiresReview = false;
    }

    private static bool HasLegacyMonitorReference(string? stableId, string? deviceName) =>
        string.IsNullOrWhiteSpace(stableId) && !string.IsNullOrWhiteSpace(deviceName);

    private bool IsPersistentIdentityMissing(string? identity) =>
        !string.IsNullOrWhiteSpace(identity) &&
        !Monitors.Any(monitor => string.Equals(monitor.IdentityKey, identity, StringComparison.OrdinalIgnoreCase));

    private static bool PersistentSelectionMatchesExactly(ApplicationSettings settings, ResolvedMonitorSelection resolved)
    {
        if (string.IsNullOrWhiteSpace(settings.GameMonitorId) || string.IsNullOrWhiteSpace(settings.CompanionMonitorId))
            return false;

        return string.Equals(resolved.GameMonitor.IdentityKey, settings.GameMonitorId, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(resolved.CompanionMonitor.IdentityKey, settings.CompanionMonitorId, StringComparison.OrdinalIgnoreCase);
    }

    private LocalizedMessage BuildStatus(ResolvedMonitorSelection selection) => new(() =>
    {
        var prefix = Localization.F($"{Monitors.Count} monitor(s) detected. Companion: {Localization.MonitorLabel(selection.CompanionMonitor)}.");
        return selection.Issues.Count == 0 ? prefix : prefix + " " + string.Join(" ", selection.Issues.Select(issue => Localization.MonitorIssue(issue)));
    });

    private void SetMonitorStatus(LocalizedMessage message)
    {
        monitorStatus = message;
        Log.Debug("Monitor status: {Status}", message.Render());
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MonitorStatus)));
    }

    private void SetMonitorError(LocalizedMessage message)
    {
        monitorError = message;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MonitorError)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasMonitorError)));
    }

    private void SetSelectionReviewMessage(LocalizedMessage message)
    {
        selectionReviewMessage = message;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectionReviewMessage)));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
