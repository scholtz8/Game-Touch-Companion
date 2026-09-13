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
    private string url = BrowserUrlPolicy.LocalHomeUrl;
    private string companionMonitor = string.Empty;
    private bool autoLaunch;
    private bool loaded;
    private bool busy;
    private bool deleteConfirmation;
    private LocalizedMessage error = string.Empty;
    private LocalizedMessage status = "Cargando perfiles…";
    private bool discardConfirmation;
    private GameProfile? pendingSelection;
    public bool DiscardConfirmation { get => discardConfirmation; private set => Set(ref discardConfirmation, value); }
    public bool HasUnsavedChanges => DisplayName != (selectedProfile?.DisplayName ?? string.Empty) ||
        ProcessName != (selectedProfile?.ProcessName ?? string.Empty) || Url != (selectedProfile?.Url ?? BrowserUrlPolicy.LocalHomeUrl) ||
        CompanionMonitor != (selectedProfile?.CompanionMonitor ?? string.Empty) || AutoLaunch != (selectedProfile?.AutoLaunch ?? false);
    public ObservableCollection<GameProfile> Profiles { get; } = [];
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
    public string Url { get => url; set => Set(ref url, value); }
    public string CompanionMonitor { get => companionMonitor; set => Set(ref companionMonitor, value); }
    public bool AutoLaunch { get => autoLaunch; set => Set(ref autoLaunch, value); }
    public bool CanEdit => loaded && !busy;
    public bool DeleteConfirmation { get => deleteConfirmation; private set => Set(ref deleteConfirmation, value); }
    public string Error => error.Render();
    private void SetError(LocalizedMessage message)
    {
        error = message;
        if (!string.IsNullOrWhiteSpace(message.Render())) Log.Warning("Profiles error: {Error}", message.Render());
        Changed(nameof(Error)); Changed(nameof(HasError));
    }

    public bool HasError => Error.Length > 0;
    public string Status => status.Render();
    private void SetStatus(LocalizedMessage message)
    {
        status = message;
        Log.Debug("Profiles status: {Status}", message.Render());
        Changed(nameof(Status));
    }

    public void RefreshLanguage()
    {
        Changed(nameof(Status));
        Changed(nameof(Error));
    }

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
            SetError(string.Empty);
            SetStatus("Perfiles listos. Autoarranque requiere activar la pestaña Detección; también puedes aplicar manualmente.");
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
            var profile = GameProfileValidation.Normalize(new GameProfile
            {
                Id = selectedProfile?.Id ?? Guid.NewGuid().ToString("N"),
                DisplayName = DisplayName, ProcessName = ProcessName, Url = Url,
                CompanionMonitor = CompanionMonitor, AutoLaunch = AutoLaunch,
            });
            var next = Profiles.ToList();
            var index = next.FindIndex(item => item.Id == profile.Id);
            if (index < 0) next.Add(profile); else next[index] = profile;
            await store.SaveAsync(new GameProfileDocument { Profiles = next });
            if (index < 0) Profiles.Add(profile); else Profiles[index] = profile;
            selectedProfile = profile;
            Changed(nameof(SelectedProfile));
            LoadDraft(profile);
            SetError(string.Empty);
            SetStatus("Perfil guardado. Aplicar usa esta versión guardada, sin ejecutar el juego.");
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
            await store.SaveAsync(new GameProfileDocument { Profiles = Profiles.Where(p => p.Id != target.Id).ToList() });
            Profiles.Remove(target);
            selectedProfile = null;
            Changed(nameof(SelectedProfile));
            LoadDraft(null);
            SetError(string.Empty);
            SetStatus("Perfil eliminado. No se cambió la página ni el monitor actual de Companion.");
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
            SetStatus("Perfil guardado aplicado manualmente. Si Companion está cerrado, ábrelo desde Pantallas o Navegador. La detección se pausa al aplicar manualmente.");
        }
        catch (InvalidDataException ex) { SetError(ex.Message); }
        catch (Exception ex) { Fail(ex, "No se pudo aplicar el perfil. Revisa Pantallas y el acceso a settings.json."); }
        finally { SetBusy(false); }
    }

    private void LoadDraft(GameProfile? profile)
    {
        CancelDiscard();
        DisplayName = profile?.DisplayName ?? string.Empty;
        ProcessName = profile?.ProcessName ?? string.Empty;
        Url = profile?.Url ?? BrowserUrlPolicy.LocalHomeUrl;
        CompanionMonitor = profile?.CompanionMonitor ?? string.Empty;
        AutoLaunch = profile?.AutoLaunch ?? false;
        DeleteConfirmation = false;
        Changed(nameof(HasUnsavedChanges));
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
        if (name is nameof(DisplayName) or nameof(ProcessName) or nameof(Url) or nameof(CompanionMonitor) or nameof(AutoLaunch))
            Changed(nameof(HasUnsavedChanges));
        return true;
    }
    private void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
