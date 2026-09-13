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
    private bool canGoBack;
    private bool canGoForward;
    private bool isReady;
    private bool settingsLoaded;
    private BrowserFavorite? selectedFavorite;

    public BrowserViewModel(IBrowserSettingsStore settingsStore) =>
        this.settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));

    public string Address { get => address; set => SetField(ref address, value); }
    public string CurrentUrl { get => currentUrl; private set => SetField(ref currentUrl, value); }
    public string HomeUrl { get => homeUrl; private set => SetField(ref homeUrl, value); }
    public string? RequestedUrl { get => requestedUrl; private set => SetField(ref requestedUrl, value); }
    public ObservableCollection<BrowserFavorite> Favorites { get; } = [];
    public BrowserFavorite? SelectedFavorite { get => selectedFavorite; set => SetField(ref selectedFavorite, value); }
    public bool ShowToolbar { get => showToolbar; private set => SetField(ref showToolbar, value); }
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
        private set { if (SetField(ref settingsLoaded, value)) OnPropertyChanged(nameof(CanEditSettings)); }
    }
    public bool CanEditSettings => SettingsLoaded;

    public void RefreshLanguage()
    {
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(Error));
        OnPropertyChanged(nameof(SettingsError));
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
            ShowToolbar = settings.ShowToolbar;
            Favorites.Clear();
            foreach (var favorite in settings.Favorites) Favorites.Add(favorite);
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
            ReportError("Introduce una dirección HTTP o HTTPS válida, sin credenciales. Otros esquemas no están permitidos.");
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
            ReportError("El favorito necesita una dirección HTTP o HTTPS válida, sin credenciales.");
            return;
        }
        if (Favorites.Any(favorite => string.Equals(favorite.Url, normalized, StringComparison.Ordinal)))
        {
            ReportStatus("Esta dirección ya está en favoritos.");
            return;
        }

        var label = string.IsNullOrWhiteSpace(title) ? new Uri(normalized).Host : title.Trim();
        if (label.Any(char.IsControl))
        {
            ReportError("El nombre del favorito no puede contener caracteres de control.");
            return;
        }
        Favorites.Add(new BrowserFavorite(label, normalized));
        await PersistAsync("Favorito guardado.");
    }

    public async Task RemoveFavoriteAsync(BrowserFavorite favorite)
    {
        ArgumentNullException.ThrowIfNull(favorite);
        if (!CanPersist() || !Favorites.Remove(favorite)) return;
        if (SelectedFavorite == favorite) SelectedFavorite = null;
        await PersistAsync("Favorito eliminado.");
    }

    public async Task SetHomeFromAddressAsync()
    {
        if (!CanPersist()) return;
        if (!BrowserUrlPolicy.TryNormalize(Address, out var normalized))
        {
            ReportError("La página inicial necesita una dirección HTTP o HTTPS válida, sin credenciales.");
            return;
        }

        HomeUrl = normalized;
        Address = normalized;
        await PersistAsync("Página inicial guardada.");
    }

    public async Task SetToolbarVisibleAsync(bool visible)
    {
        // The recovery control must still work if preferences cannot be read or saved.
        ShowToolbar = visible;
        if (CanPersist()) await PersistAsync(visible ? "Barra de navegación visible." : "Barra oculta. Pulsa Mostrar barra para recuperarla.");
    }

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
                ShowToolbar = ShowToolbar,
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
