using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using GameTouchCompanion.Core;
using Serilog;

namespace GameTouchCompanion.App;

/// <summary>Browser state shared by configuration and the non-activating Companion window.</summary>
public sealed class BrowserViewModel : INotifyPropertyChanged
{
    private readonly IBrowserSettingsStore settingsStore;
    private readonly SemaphoreSlim saveGate = new(1, 1);
    private string address = BrowserUrlPolicy.LocalHomeUrl;
    private string currentUrl = string.Empty;
    private string homeUrl = BrowserUrlPolicy.LocalHomeUrl;
    private string? requestedUrl;
    private LocalizedMessage status = "Configura una dirección y abre Companion.";
    private LocalizedMessage error = string.Empty;
    private LocalizedMessage settingsError = string.Empty;
    private bool showToolbar = true;
    private bool startWithToolbarVisible = true;
    private bool openNewWindowsInTabs = true;
    private bool activateNewWindowTabs = true;
    private bool canGoBack;
    private bool canGoForward;
    private bool isReady;
    private bool settingsLoaded;
    private BrowserFavorite? selectedFavorite;
    private BrowserFavorite? editingFavorite;
    private string favoriteTitle = string.Empty;

    public BrowserViewModel(IBrowserSettingsStore settingsStore) =>
        this.settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));

    public string Address { get => address; set => SetField(ref address, value); }
    public string CurrentUrl { get => currentUrl; private set => SetField(ref currentUrl, value); }
    public string HomeUrl { get => homeUrl; private set => SetField(ref homeUrl, value); }
    public string? RequestedUrl { get => requestedUrl; private set => SetField(ref requestedUrl, value); }
    public ObservableCollection<BrowserFavorite> Favorites { get; } = [];
    public BrowserFavorite? SelectedFavorite
    {
        get => selectedFavorite;
        set
        {
            if (!SetField(ref selectedFavorite, value)) return;
            OnPropertyChanged(nameof(CanEditSelectedFavorite));
        }
    }
    public string FavoriteTitle { get => favoriteTitle; set => SetField(ref favoriteTitle, value); }
    public bool IsEditingFavorite => editingFavorite is not null;
    public bool CanEditSelectedFavorite => CanEditSettings && SelectedFavorite is not null;
    public string FavoriteActionText => Localization.Get(IsEditingFavorite ? "BrowserSaveFavoriteChanges" : "Ui086");
    public bool ShowToolbar { get => showToolbar; private set => SetField(ref showToolbar, value); }
    public bool StartWithToolbarVisible { get => startWithToolbarVisible; private set => SetField(ref startWithToolbarVisible, value); }
    public bool OpenNewWindowsInTabs
    {
        get => openNewWindowsInTabs;
        private set
        {
            if (SetField(ref openNewWindowsInTabs, value)) OnPropertyChanged(nameof(CanEditNewWindowActivation));
        }
    }
    public bool ActivateNewWindowTabs { get => activateNewWindowTabs; private set => SetField(ref activateNewWindowTabs, value); }
    public bool CanGoBack { get => canGoBack; private set => SetField(ref canGoBack, value); }
    public bool CanGoForward { get => canGoForward; private set => SetField(ref canGoForward, value); }
    public bool IsReady { get => isReady; private set => SetField(ref isReady, value); }
    public string Status => status.Render();
    private void SetStatus(LocalizedMessage message)
    {
        status = message;
        Log.Debug("Browser status: {Status}", message.Render());
        OnPropertyChanged(nameof(Status));
    }

    public string Error => error.Render();
    private void SetError(LocalizedMessage message)
    {
        error = message;
        if (!string.IsNullOrWhiteSpace(message.Render())) Log.Warning("Browser error: {Error}", message.Render());
        OnPropertyChanged(nameof(Error)); OnPropertyChanged(nameof(HasError));
    }

    public bool HasError => Error.Length > 0;
    public string SettingsError => settingsError.Render();
    private void SetSettingsError(LocalizedMessage message)
    {
        settingsError = message;
        OnPropertyChanged(nameof(SettingsError)); OnPropertyChanged(nameof(HasSettingsError));
    }

    public bool HasSettingsError => SettingsError.Length > 0;
    public bool SettingsLoaded
    {
        get => settingsLoaded;
        private set
        {
            if (!SetField(ref settingsLoaded, value)) return;
            OnPropertyChanged(nameof(CanEditSettings));
            OnPropertyChanged(nameof(CanEditNewWindowActivation));
            OnPropertyChanged(nameof(CanEditSelectedFavorite));
        }
    }
    public bool CanEditSettings => SettingsLoaded;
    public bool CanEditNewWindowActivation => CanEditSettings && OpenNewWindowsInTabs;

    public void RefreshLanguage()
    {
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(Error));
        OnPropertyChanged(nameof(SettingsError));
        OnPropertyChanged(nameof(FavoriteActionText));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action<string>? NavigationRequested;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = await settingsStore.LoadAsync(cancellationToken);
            HomeUrl = settings.HomeUrl;
            Address = HomeUrl;
            StartWithToolbarVisible = settings.ShowToolbar;
            ShowToolbar = StartWithToolbarVisible;
            OpenNewWindowsInTabs = settings.OpenNewWindowsInTabs;
            ActivateNewWindowTabs = settings.ActivateNewWindowTabs;
            Favorites.Clear();
            foreach (var favorite in settings.Favorites) Favorites.Add(favorite);
            SetEditingFavorite(null);
            FavoriteTitle = string.Empty;
            SelectedFavorite = null;
            SettingsLoaded = true;
            SetSettingsError(string.Empty);
            ReportStatus("Navegador preparado. Abre Companion para navegar.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            SettingsLoaded = false;
            Log.Warning("Browser settings load failed. Type={Type}; Code={Code}", ex.GetType().Name, ex.HResult);
            SetSettingsError("No se pudo leer browser.json. Corrige el archivo y reinicia la aplicación. Los cambios de preferencias están bloqueados para conservarlo.");
            ReportError(settingsError);
        }
    }

    public bool NavigateAddress() => Navigate(Address);

    public bool Navigate(string input)
    {
        if (!BrowserUrlPolicy.TryNormalize(input, out var normalized))
        {
            ReportError("Introduce una dirección web válida. Puedes usar un dominio, localhost, una IP o una URL HTTP/HTTPS, sin credenciales.");
            return false;
        }

        Address = normalized;
        RequestedUrl = normalized;
        ReportStatus(IsReady ? "Cargando página…" : "Dirección preparada; se abrirá al iniciar Companion.");
        NavigationRequested?.Invoke(normalized);
        return true;
    }

    public bool GoHome() => Navigate(HomeUrl);

    public Task AddFavoriteAsync() => AddFavoriteAsync(CurrentUrl.Length > 0 ? CurrentUrl : Address);

    public async Task AddFavoriteAsync(string url, string? title = null)
    {
        if (!CanPersist()) return;
        if (!BrowserUrlPolicy.TryNormalize(url, out var normalized))
        {
            ReportError("El favorito necesita una dirección web válida, como un dominio, localhost, una IP o una URL HTTP/HTTPS, sin credenciales.");
            return;
        }
        if (Favorites.Any(favorite => string.Equals(favorite.Url, normalized, StringComparison.Ordinal)))
        {
            ReportStatus("Esta dirección ya está en favoritos.");
            return;
        }

        var label = string.IsNullOrWhiteSpace(title) ? string.Empty : title.Trim();
        if (label.Any(char.IsControl))
        {
            ReportError("El nombre del favorito no puede contener caracteres de control.");
            return;
        }
        Favorites.Add(new BrowserFavorite(label, normalized));
        await PersistAsync("Favorito guardado.");
    }


    public void BeginEditSelectedFavorite()
    {
        if (!CanEditSelectedFavorite || SelectedFavorite is null) return;

        SetEditingFavorite(SelectedFavorite);
        Address = SelectedFavorite.Url;
        FavoriteTitle = SelectedFavorite.Title ?? string.Empty;
    }

    public void CancelFavoriteEdit()
    {
        if (!IsEditingFavorite) return;
        SetEditingFavorite(null);
        FavoriteTitle = string.Empty;
    }

    public async Task UpdateEditingFavoriteAsync(string url, string? title)
    {
        if (!CanPersist() || editingFavorite is null) return;
        if (!BrowserUrlPolicy.TryNormalize(url, out var normalized))
        {
            ReportError("El favorito necesita una dirección web válida, como un dominio, localhost, una IP o una URL HTTP/HTTPS, sin credenciales.");
            return;
        }

        var index = Favorites.IndexOf(editingFavorite);
        if (index < 0)
        {
            SetEditingFavorite(null);
            return;
        }

        if (Favorites.Where((_, favoriteIndex) => favoriteIndex != index)
            .Any(favorite => string.Equals(favorite.Url, normalized, StringComparison.Ordinal)))
        {
            ReportStatus("Esta dirección ya está en favoritos.");
            return;
        }

        var label = string.IsNullOrWhiteSpace(title) ? string.Empty : title.Trim();
        if (label.Any(char.IsControl))
        {
            ReportError("El nombre del favorito no puede contener caracteres de control.");
            return;
        }

        var updated = new BrowserFavorite(label, normalized);
        Favorites[index] = updated;
        SelectedFavorite = updated;
        SetEditingFavorite(updated);
        await PersistAsync("Favorito actualizado.");

        if (!HasSettingsError)
        {
            SetEditingFavorite(null);
            FavoriteTitle = string.Empty;
        }
    }

    public async Task RemoveFavoriteAsync(BrowserFavorite favorite)
    {
        ArgumentNullException.ThrowIfNull(favorite);
        if (!CanPersist() || !Favorites.Remove(favorite)) return;
        if (SelectedFavorite == favorite) SelectedFavorite = null;
        if (editingFavorite == favorite)
        {
            SetEditingFavorite(null);
            FavoriteTitle = string.Empty;
        }
        await PersistAsync("Favorito eliminado.");
    }

    public async Task SetHomeFromAddressAsync()
    {
        if (!CanPersist()) return;
        if (!BrowserUrlPolicy.TryNormalize(Address, out var normalized))
        {
            ReportError("La página inicial necesita una dirección web válida, como un dominio, localhost, una IP o una URL HTTP/HTTPS, sin credenciales.");
            return;
        }

        HomeUrl = normalized;
        Address = normalized;
        await PersistAsync("Página inicial guardada.");
    }

    public Task SetToolbarVisibleAsync(bool visible)
    {
        // Runtime visibility is intentionally session-only. Personalization controls
        // whether a newly opened Companion starts with the toolbar visible.
        ShowToolbar = visible;
        ReportStatus(visible ? "Barra de navegación visible." : "Barra oculta. Pulsa Mostrar barra para recuperarla.");
        return Task.CompletedTask;
    }

    public async Task SetStartToolbarVisibleAsync(bool visible)
    {
        if (!CanPersist()) return;
        StartWithToolbarVisible = visible;
        await PersistAsync(visible
            ? "Companion iniciará con la barra táctil visible."
            : "Companion iniciará con la barra táctil oculta.");
    }

    public async Task SetOpenNewWindowsInTabsAsync(bool enabled)
    {
        if (!CanPersist()) return;
        OpenNewWindowsInTabs = enabled;
        await PersistAsync(enabled
            ? "Las ventanas nuevas se abrirán como pestañas de Companion."
            : "Las ventanas nuevas reutilizarán la pestaña actual de Companion.");
    }

    public async Task SetActivateNewWindowTabsAsync(bool enabled)
    {
        if (!CanPersist()) return;
        ActivateNewWindowTabs = enabled;
        await PersistAsync(enabled
            ? "Las pestañas nuevas se activarán al abrirse."
            : "Las pestañas nuevas se abrirán en segundo plano.");
    }

    public void ResetRuntimeToolbarToPreference() => ShowToolbar = StartWithToolbarVisible;

    public void ReportReady()
    {
        IsReady = true;
        ReportStatus("Navegador listo.");
    }

    public void ReportNavigation(string url)
    {
        if (!BrowserUrlPolicy.IsAllowed(url)) return;
        CurrentUrl = url;
        RequestedUrl = url;
        // Do not overwrite an address being edited in the Configuration window.
    }

    public void ReportHistory(bool back, bool forward)
    {
        CanGoBack = IsReady && back;
        CanGoForward = IsReady && forward;
    }

    public void ReportStatus(LocalizedMessage message)
    {
        // Runtime status changes must not hide a persistent browser-settings load/save error.
        if (!HasSettingsError)
            SetError(string.Empty);
        SetStatus(message);
    }

    public void ReportError(LocalizedMessage message)
    {
        SetError(message);
        SetStatus(message);
    }

    public void ReportClosed()
    {
        IsReady = false;
        CanGoBack = false;
        CanGoForward = false;
        ReportStatus("Companion cerrado. Puedes volver a abrirlo desde Configuración.");
    }


    private void SetEditingFavorite(BrowserFavorite? favorite)
    {
        if (ReferenceEquals(editingFavorite, favorite)) return;
        editingFavorite = favorite;
        OnPropertyChanged(nameof(IsEditingFavorite));
        OnPropertyChanged(nameof(FavoriteActionText));
    }

    private bool CanPersist()
    {
        if (CanEditSettings) return true;
        ReportError("Las preferencias no se cargaron. Corrige browser.json y reinicia antes de guardar cambios.");
        return false;
    }

    private async Task PersistAsync(string successMessage)
    {
        await saveGate.WaitAsync();
        try
        {
            await settingsStore.SaveAsync(new BrowserSettings
            {
                HomeUrl = HomeUrl,
                ShowToolbar = StartWithToolbarVisible,
                OpenNewWindowsInTabs = OpenNewWindowsInTabs,
                ActivateNewWindowTabs = ActivateNewWindowTabs,
                Favorites = [.. Favorites],
            });
            SetSettingsError(string.Empty);
            ReportStatus(successMessage);
        }
        catch (Exception ex)
        {
            Log.Warning("Browser settings save failed. Type={Type}; Code={Code}", ex.GetType().Name, ex.HResult);
            SetSettingsError("No se pudieron guardar las preferencias. Los cambios actuales solo están en memoria; revisa el acceso a browser.json.");
            ReportError(settingsError);
        }
        finally { saveGate.Release(); }
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
