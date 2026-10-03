namespace GameTouchCompanion.Core;

public sealed record BrowserSettings
{
    public string HomeUrl { get; init; } = BrowserUrlPolicy.LocalHomeUrl;

    public bool ShowToolbar { get; init; } = true;

    public bool OpenNewWindowsInTabs { get; init; } = true;

    public bool ActivateNewWindowTabs { get; init; } = true;

    public List<BrowserFavorite> Favorites { get; init; } = [];
}
