using System.Text.Json;

namespace GameTouchCompanion.Core;

public sealed class JsonGameProfileStore : IGameProfileStore
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    public JsonGameProfileStore(string? filePath = null)
    {
        if (filePath is not null) ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        FilePath = Path.GetFullPath(filePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GameTouchCompanion", "profiles.json"));
    }
    public string FilePath { get; }

    public async Task<GameProfileDocument> LoadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            GameProfileDocument? document;
            await using (var stream = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read,
                4096, FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                document = await JsonSerializer.DeserializeAsync<GameProfileDocument>(stream, Options, cancellationToken).ConfigureAwait(false);
            }
            var originalVersion = document?.SchemaVersion ?? 0;
            var normalized = GameProfileValidation.Normalize(document);
            if (originalVersion < GameProfileValidation.CurrentSchemaVersion)
                await SaveAsync(normalized, cancellationToken).ConfigureAwait(false);
            return normalized;
        }
        catch (FileNotFoundException) { return new GameProfileDocument(); }
        catch (DirectoryNotFoundException) { return new GameProfileDocument(); }
    }

    public async Task SaveAsync(GameProfileDocument document, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var snapshot = GameProfileValidation.Normalize(document);
        var directory = Path.GetDirectoryName(FilePath)!;
        Directory.CreateDirectory(directory);
        var temporary = Path.Combine(directory, $".profiles.{Guid.NewGuid():N}.tmp");
        try
        {
            await using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                4096, FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await JsonSerializer.SerializeAsync(stream, snapshot, Options, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporary, FilePath, overwrite: true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
