using System.Text.Json;

namespace GameTouchCompanion.Core;

public sealed class JsonBrowserSettingsStore : IBrowserSettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    public JsonBrowserSettingsStore(string? filePath = null)
    {
        if (filePath is not null && string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("The browser settings path cannot be empty.", nameof(filePath));
        }

        FilePath = Path.GetFullPath(filePath ?? GetDefaultFilePath());
    }

    public string FilePath { get; }

    public static string GetDefaultFilePath()
    {
        var localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localApplicationData))
        {
            throw new InvalidOperationException("The local application data directory is unavailable.");
        }

        return Path.Combine(localApplicationData, "GameTouchCompanion", "browser.json");
    }

    public async Task<BrowserSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            await using var stream = new FileStream(
                FilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            var settings = await JsonSerializer
                .DeserializeAsync<BrowserSettings>(stream, SerializerOptions, cancellationToken)
                .ConfigureAwait(false);

            return ValidateAndNormalize(settings);
        }
        catch (FileNotFoundException)
        {
            return new BrowserSettings();
        }
        catch (DirectoryNotFoundException)
        {
            return new BrowserSettings();
        }
    }

    public async Task SaveAsync(BrowserSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        cancellationToken.ThrowIfCancellationRequested();
        var validatedSettings = ValidateAndNormalize(settings);

        var directoryPath = Path.GetDirectoryName(FilePath)
            ?? throw new InvalidOperationException("The browser settings path has no parent directory.");
        Directory.CreateDirectory(directoryPath);

        var temporaryFilePath = Path.Combine(
            directoryPath,
            $".{Path.GetFileName(FilePath)}.{Guid.NewGuid():N}.tmp");

        try
        {
            await using (var stream = new FileStream(
                temporaryFilePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 4096,
                FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await JsonSerializer
                    .SerializeAsync(stream, validatedSettings, SerializerOptions, cancellationToken)
                    .ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporaryFilePath, FilePath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryFilePath))
            {
                File.Delete(temporaryFilePath);
            }
        }
    }

    private static BrowserSettings ValidateAndNormalize(BrowserSettings? settings)
    {
        if (settings is null)
        {
            throw new InvalidDataException("Browser settings must contain a JSON object, not null.");
        }

        if (!BrowserUrlPolicy.TryNormalize(settings.HomeUrl, out var homeUrl))
        {
            throw new InvalidDataException("The browser home URL must be an allowed absolute HTTP(S) URL.");
        }

        if (settings.Favorites is null)
        {
            throw new InvalidDataException("The browser favorites list cannot be null.");
        }

        var favorites = new List<BrowserFavorite>(settings.Favorites.Count);
        foreach (var favorite in settings.Favorites)
        {
            if (favorite is null || string.IsNullOrWhiteSpace(favorite.Title) ||
                favorite.Title.Any(char.IsControl) ||
                !BrowserUrlPolicy.TryNormalize(favorite.Url, out var url))
            {
                throw new InvalidDataException("Each browser favorite must have a title and an allowed absolute HTTP(S) URL.");
            }

            favorites.Add(new BrowserFavorite(favorite.Title.Trim(), url));
        }

        return settings with { HomeUrl = homeUrl, Favorites = favorites };
    }
}
