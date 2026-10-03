namespace GameTouchCompanion.Core;

public sealed record BrowserSettings
{
    public string HomeUrl { get; init; } = BrowserUrlPolicy.LocalHomeUrl;

    public bool ShowToolbar { get; init; } = true;

    public bool OpenNewWindowsInTabs { get; init; } = true;

    public bool ActivateNewWindowTabs { get; init; } = true;

    public int DefaultZoomPercent { get; init; } = BrowserZoomPolicy.DefaultPercent;

    public bool RememberZoomPerSite { get; init; }

    public Dictionary<string, int> SiteZoomPercentages { get; init; } = [];

    public List<BrowserFavorite> Favorites { get; init; } = [];
}
