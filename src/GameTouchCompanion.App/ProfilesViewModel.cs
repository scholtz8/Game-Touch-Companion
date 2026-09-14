using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using GameTouchCompanion.Core;
using Serilog;

namespace GameTouchCompanion.App;

public sealed class ProfilesViewModel(IGameProfileStore store) : INotifyPropertyChanged
{
    private GameProfile? selectedProfile;
    private string displayName = string.Empty;
    private string processName = string.Empty;
    private string companionMonitor = string.Empty;
    private bool autoLaunch;
    private bool loaded;
    private bool busy;
    private bool deleteConfirmation;
    private LocalizedMessage error = string.Empty;
    private LocalizedMessage status = "Cargando perfiles…";
    private bool discardConfirmation;
    private GameProfile? pendingSelection;
    private ProfileTabDraft? selectedTab;
    private bool draftBaselineCaptured;
    private string baselineDisplayName = string.Empty;
    private string baselineProcessName = string.Empty;
    private string baselineCompanionMonitor = string.Empty;
    private bool baselineAutoLaunch;
    private readonly List<ProfileTabBaseline> baselineTabs = [];

    public bool DiscardConfirmation { get => discardConfirmation; private set => Set(ref discardConfirmation, value); }
    public ObservableCollection<GameProfile> Profiles { get; } = [];
    public ObservableCollection<ProfileTabDraft> Tabs { get; } = [];
    public ProfileTabDraft? SelectedTab { get => selectedTab; set => Set(ref selectedTab, value); }

    public bool HasUnsavedChanges
    {
        get
        {
            if (!draftBaselineCaptured) return false;
            if (DisplayName != baselineDisplayName ||
                ProcessName != baselineProcessName ||
                CompanionMonitor != baselineCompanionMonitor ||
                AutoLaunch != baselineAutoLaunch ||
                Tabs.Count != baselineTabs.Count) return true;
            for (var i = 0; i < Tabs.Count; i++)
            {
                var draft = Tabs[i];
                var baseline = baselineTabs[i];
                if (draft.Id != baseline.Id || draft.Name != baseline.Name || draft.Url != baseline.Url ||
                    draft.IsPrimary != baseline.IsPrimary) return true;
            }
            return false;
        }
    }

    public GameProfile? SelectedProfile
    {
        get => selectedProfile;
        set
        {
            if (busy || selectedProfile == value) return;
            RequestSelection(value);
        }
    }
    public string DisplayName { get => displayName; set => Set(ref displayName, value); }
    public string ProcessName { get => processName; set => Set(ref processName, value); }
    public string CompanionMonitor { get => companionMonitor; set => Set(ref companionMonitor, value); }
    public bool AutoLaunch { get => autoLaunch; set => Set(ref autoLaunch, value); }
    public bool CanEdit => loaded && !busy;
    public bool DeleteConfirmation { get => deleteConfirmation; private set => Set(ref deleteConfirmation, value); }
    public string Error => error.Render();
    public bool HasError => Error.Length > 0;
    public string Status => status.Render();

    public event PropertyChangedEventHandler? PropertyChanged;

    public async Task InitializeAsync()
    {
        SetBusy(true);
        try
        {
            var document = await store.LoadAsync();
            Profiles.Clear();
            foreach (var profile in document.Profiles) Profiles.Add(profile);
            loaded = true;
            LoadDraft(null);
            SetError(string.Empty);
            SetStatus("Perfiles listos. Puedes asignar varias pestañas a cada perfil y marcar una como principal.");
        }
        catch (Exception ex)
        {
            loaded = false;
            Fail(ex, "No se pudo leer profiles.json. Se conserva el archivo: corrígelo y reinicia para editar perfiles.");
        }
        finally { SetBusy(false); }
    }

    public void NewProfile()
    {
        if (!CanEdit) return;
        RequestSelection(null);
    }

    public void AddTab()
    {
        if (!CanEdit || Tabs.Count >= 20) return;
        var draft = new ProfileTabDraft
        {
            Name = Localization.F($"Página {Tabs.Count + 1}"),
            Url = BrowserUrlPolicy.LocalHomeUrl,
            IsPrimary = Tabs.Count == 0
        };
        AttachTab(draft);
        Tabs.Add(draft);
        SelectedTab = draft;
        Changed(nameof(HasUnsavedChanges));
    }

    public void RemoveTab(ProfileTabDraft? tab)
    {
        if (!CanEdit || tab is null || Tabs.Count <= 1 || !Tabs.Remove(tab)) return;
        tab.PropertyChanged -= TabChanged;
        if (tab.IsPrimary && Tabs.Count > 0) SetPrimary(Tabs[0]);
        SelectedTab = Tabs.FirstOrDefault();
        Changed(nameof(HasUnsavedChanges));
    }

    public void SetPrimary(ProfileTabDraft? tab)
    {
        if (!CanEdit || tab is null || !Tabs.Contains(tab)) return;
        foreach (var item in Tabs) item.IsPrimary = ReferenceEquals(item, tab);
        Changed(nameof(HasUnsavedChanges));
    }

    public void MoveTab(ProfileTabDraft? tab, int offset)
    {
        if (!CanEdit || tab is null || offset == 0) return;
        var index = Tabs.IndexOf(tab);
        var next = index + offset;
        if (index < 0 || next < 0 || next >= Tabs.Count) return;
        Tabs.Move(index, next);
        Changed(nameof(HasUnsavedChanges));
    }

    private void RequestSelection(GameProfile? value)
    {
        if (HasUnsavedChanges)
        {
            pendingSelection = value;
            DiscardConfirmation = true;
            DeleteConfirmation = false;
            Changed(nameof(SelectedProfile));
            return;
        }
        SelectDraft(value);
    }

    private void SelectDraft(GameProfile? value)
    {
        selectedProfile = value;
        Changed(nameof(SelectedProfile));
        LoadDraft(value);
        SetError(string.Empty);
        SetStatus(value is null ? "Nuevo borrador; pulsa Guardar para crearlo." : "Perfil seleccionado; Aplicar usa la versión guardada.");
    }

    public void CancelDiscard() { pendingSelection = null; DiscardConfirmation = false; Changed(nameof(SelectedProfile)); }
    public void ConfirmDiscard()
    {
        if (!CanEdit || !DiscardConfirmation) return;
        var target = pendingSelection;
        CancelDiscard();
        SelectDraft(target);
    }

    public async Task SaveAsync()
    {
        if (!CanEdit) return;
        SetBusy(true);
        try
        {
            if (Tabs.Count == 0) throw new InvalidDataException("Añade al menos una pestaña al perfil.");
            var primary = Tabs.FirstOrDefault(t => t.IsPrimary) ?? Tabs[0];
            var tabModels = Tabs.Select((tab, index) => new GameProfileTab
            {
                Id = tab.Id,
                Name = tab.Name,
                Url = tab.Url,
                Order = index
            }).ToList();
            var profile = GameProfileValidation.Normalize(new GameProfile
            {
                Id = selectedProfile?.Id ?? Guid.NewGuid().ToString("N"),
                DisplayName = DisplayName,
                ProcessName = ProcessName,
                Url = primary.Url,
                Tabs = tabModels,
                PrimaryTabId = primary.Id,
                CompanionMonitor = CompanionMonitor,
                AutoLaunch = AutoLaunch,
            });
            var next = Profiles.ToList();
            var index = next.FindIndex(item => item.Id == profile.Id);
            if (index < 0) next.Add(profile); else next[index] = profile;
            await store.SaveAsync(new GameProfileDocument { SchemaVersion = GameProfileValidation.CurrentSchemaVersion, Profiles = next });
            if (index < 0) Profiles.Add(profile); else Profiles[index] = profile;
            selectedProfile = profile;
            Changed(nameof(SelectedProfile));
            LoadDraft(profile);
            SetError(string.Empty);
            SetStatus("Perfil guardado con sus pestañas. Aplicar usa esta versión guardada.");
        }
        catch (InvalidDataException ex) { SetError(ex.Message); }
        catch (Exception ex) { Fail(ex, "No se pudo guardar profiles.json. El borrador sigue disponible; el perfil guardado no cambió."); }
        finally { SetBusy(false); }
    }

    public void RequestDelete()
    {
        if (CanEdit && SelectedProfile is not null) DeleteConfirmation = true;
    }
    public void CancelDelete() => DeleteConfirmation = false;
    public async Task DeleteAsync()
    {
        if (!CanEdit || !DeleteConfirmation || SelectedProfile is null) return;
        var target = SelectedProfile;
        SetBusy(true);
        try
        {
            await store.SaveAsync(new GameProfileDocument { SchemaVersion = GameProfileValidation.CurrentSchemaVersion, Profiles = Profiles.Where(p => p.Id != target.Id).ToList() });
            Profiles.Remove(target);
            selectedProfile = null;
            Changed(nameof(SelectedProfile));
            LoadDraft(null);
            SetError(string.Empty);
            SetStatus("Perfil eliminado. No se cambió la sesión actual de Companion.");
        }
        catch (Exception ex) { Fail(ex, "No se pudo eliminar el perfil; se conserva en la colección guardada."); }
        finally { SetBusy(false); }
    }

    public async Task ApplyAsync(Func<GameProfile, Task> apply)
    {
        if (!CanEdit || SelectedProfile is null) return;
        var saved = SelectedProfile;
        SetBusy(true);
        try
        {
            await apply(saved);
            SetError(string.Empty);
            SetStatus("Perfil guardado aplicado manualmente. Si Companion está abierto, su sesión de pestañas se actualiza.");
        }
        catch (InvalidDataException ex) { SetError(ex.Message); }
        catch (Exception ex) { Fail(ex, "No se pudo aplicar el perfil. Revisa Pantallas y el acceso a settings.json."); }
        finally { SetBusy(false); }
    }

    public void RefreshLanguage()
    {
        Changed(nameof(Status));
        Changed(nameof(Error));
    }

    private void LoadDraft(GameProfile? profile)
    {
        CancelDiscard();
        DisplayName = profile?.DisplayName ?? string.Empty;
        ProcessName = profile?.ProcessName ?? string.Empty;
        CompanionMonitor = profile?.CompanionMonitor ?? string.Empty;
        AutoLaunch = profile?.AutoLaunch ?? false;
        foreach (var item in Tabs) item.PropertyChanged -= TabChanged;
        Tabs.Clear();
        var sourceTabs = profile?.Tabs.OrderBy(t => t.Order).ToList();
        if (sourceTabs is null || sourceTabs.Count == 0)
        {
            var draft = new ProfileTabDraft { Name = Localization.F($"Página {1}"), Url = profile?.Url ?? BrowserUrlPolicy.LocalHomeUrl, IsPrimary = true };
            AttachTab(draft);
            Tabs.Add(draft);
        }
        else
        {
            foreach (var tab in sourceTabs)
            {
                var draft = new ProfileTabDraft
                {
                    Id = tab.Id,
                    Name = tab.Name,
                    Url = tab.Url,
                    IsPrimary = string.Equals(tab.Id, profile?.PrimaryTabId, StringComparison.OrdinalIgnoreCase)
                };
                AttachTab(draft);
                Tabs.Add(draft);
            }
            if (!Tabs.Any(t => t.IsPrimary) && Tabs.Count > 0) Tabs[0].IsPrimary = true;
        }
        SelectedTab = Tabs.FirstOrDefault();
        DeleteConfirmation = false;
        CaptureDraftBaseline();
        Changed(nameof(HasUnsavedChanges));
    }

    private void CaptureDraftBaseline()
    {
        baselineDisplayName = DisplayName;
        baselineProcessName = ProcessName;
        baselineCompanionMonitor = CompanionMonitor;
        baselineAutoLaunch = AutoLaunch;
        baselineTabs.Clear();
        foreach (var tab in Tabs)
            baselineTabs.Add(new ProfileTabBaseline(tab.Id, tab.Name, tab.Url, tab.IsPrimary));
        draftBaselineCaptured = true;
    }

    private sealed record ProfileTabBaseline(string Id, string Name, string Url, bool IsPrimary);

    private void AttachTab(ProfileTabDraft tab) => tab.PropertyChanged += TabChanged;
    private void TabChanged(object? sender, PropertyChangedEventArgs e) => Changed(nameof(HasUnsavedChanges));
    private void SetError(LocalizedMessage message)
    {
        error = message;
        if (!string.IsNullOrWhiteSpace(message.Render())) Log.Warning("Profiles error: {Error}", message.Render());
        Changed(nameof(Error)); Changed(nameof(HasError));
    }
    private void SetStatus(LocalizedMessage message)
    {
        status = message;
        Log.Debug("Profiles status: {Status}", message.Render());
        Changed(nameof(Status));
    }
    private void Fail(Exception ex, string message)
    {
        Log.Warning("Profile operation failed. Type={Type}; Code={Code}", ex.GetType().Name, ex.HResult);
        SetError(message);
    }
    private void SetBusy(bool value) { busy = value; Changed(nameof(CanEdit)); }
    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        Changed(name);
        if (name is nameof(DisplayName) or nameof(ProcessName) or nameof(CompanionMonitor) or nameof(AutoLaunch))
            Changed(nameof(HasUnsavedChanges));
        return true;
    }
    private void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
