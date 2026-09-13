namespace GameTouchCompanion.Core;

public interface IApplicationSettingsStore
{
    string FilePath { get; }

    Task<ApplicationSettings> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(ApplicationSettings settings, CancellationToken cancellationToken = default);
}
