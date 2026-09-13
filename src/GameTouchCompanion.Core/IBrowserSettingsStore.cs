namespace GameTouchCompanion.Core;

public interface IBrowserSettingsStore
{
    string FilePath { get; }

    Task<BrowserSettings> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(BrowserSettings settings, CancellationToken cancellationToken = default);
}
