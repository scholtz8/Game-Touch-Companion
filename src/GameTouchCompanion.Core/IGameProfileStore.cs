namespace GameTouchCompanion.Core;

public interface IGameProfileStore
{
    string FilePath { get; }
    Task<GameProfileDocument> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(GameProfileDocument document, CancellationToken cancellationToken = default);
}
