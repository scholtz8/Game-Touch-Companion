using System.Text.Json;

namespace GameTouchCompanion.Core;

public sealed class JsonApplicationSettingsStore : IApplicationSettingsStore
{
    private const string ApplicationDirectoryName = "GameTouchCompanion";
    private const string SettingsFileName = "settings.json";

    private readonly JsonSerializerOptions serializerOptions;

    public JsonApplicationSettingsStore(
        string? filePath = null,
        JsonSerializerOptions? serializerOptions = null)
    {
        if (filePath is not null && string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("The settings path cannot be empty.", nameof(filePath));
        }

        FilePath = Path.GetFullPath(filePath ?? GetDefaultFilePath());
        this.serializerOptions = serializerOptions is null
            ? CreateDefaultSerializerOptions()
            : new JsonSerializerOptions(serializerOptions);
    }

    public string FilePath { get; }

    public static string GetDefaultFilePath()
    {
        var localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localApplicationData))
        {
            throw new InvalidOperationException("The local application data directory is unavailable.");
        }

        return Path.Combine(localApplicationData, ApplicationDirectoryName, SettingsFileName);
    }

    public async Task<ApplicationSettings> LoadAsync(CancellationToken cancellationToken = default)
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

            return await JsonSerializer
                .DeserializeAsync<ApplicationSettings>(stream, serializerOptions, cancellationToken)
                .ConfigureAwait(false) ?? new ApplicationSettings();
        }
        catch (FileNotFoundException)
        {
            return new ApplicationSettings();
        }
        catch (DirectoryNotFoundException)
        {
            return new ApplicationSettings();
        }
    }

    public async Task SaveAsync(
        ApplicationSettings settings,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        cancellationToken.ThrowIfCancellationRequested();

        var directoryPath = Path.GetDirectoryName(FilePath)
            ?? throw new InvalidOperationException("The settings path has no parent directory.");
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
                    .SerializeAsync(stream, settings, serializerOptions, cancellationToken)
                    .ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

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

    private static JsonSerializerOptions CreateDefaultSerializerOptions() =>
        new(JsonSerializerDefaults.Web)
        {
            WriteIndented = true,
        };
}
