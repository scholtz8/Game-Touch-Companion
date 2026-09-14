using System.Text.Json.Serialization;

namespace GameTouchCompanion.Core;

public sealed record GameProfileTab
{
    [JsonRequired] public string Id { get; init; } = Guid.NewGuid().ToString("N");
    [JsonRequired] public string Name { get; init; } = "Página";
    [JsonRequired] public string Url { get; init; } = BrowserUrlPolicy.LocalHomeUrl;
    public int Order { get; init; }
}

public sealed record GameProfile
{
    [JsonRequired] public string Id { get; init; } = Guid.NewGuid().ToString("N");
    [JsonRequired] public string DisplayName { get; init; } = string.Empty;
    public string ProcessName { get; init; } = string.Empty;

    // Legacy compatibility: kept in schema v2 so older code/readers can still identify
    // the profile's primary address. New code should use Tabs + PrimaryTabId.
    [JsonRequired] public string Url { get; init; } = BrowserUrlPolicy.LocalHomeUrl;
    public List<GameProfileTab> Tabs { get; init; } = [];
    public string? PrimaryTabId { get; init; }

    public string? CompanionMonitor { get; init; }
    public bool AutoLaunch { get; init; }
    public bool NoActivate { get; init; } = true;
    public bool RestoreGameFocusFallback { get; init; }

    [JsonIgnore]
    public GameProfileTab PrimaryTab => Tabs.FirstOrDefault(tab =>
        string.Equals(tab.Id, PrimaryTabId, StringComparison.OrdinalIgnoreCase)) ??
        Tabs.OrderBy(tab => tab.Order).First();
}

public sealed record GameProfileDocument
{
    [JsonRequired] public int SchemaVersion { get; init; } = 2;
    [JsonRequired] public List<GameProfile> Profiles { get; init; } = [];
}
