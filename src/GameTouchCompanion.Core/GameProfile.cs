using System.Text.Json.Serialization;

namespace GameTouchCompanion.Core;

public sealed record GameProfile
{
    [JsonRequired] public string Id { get; init; } = Guid.NewGuid().ToString("N");
    [JsonRequired] public string DisplayName { get; init; } = string.Empty;
    public string ProcessName { get; init; } = string.Empty;
    [JsonRequired] public string Url { get; init; } = BrowserUrlPolicy.LocalHomeUrl;
    public string? CompanionMonitor { get; init; }
    public bool AutoLaunch { get; init; }
    public bool NoActivate { get; init; } = true;
    public bool RestoreGameFocusFallback { get; init; }
}

public sealed record GameProfileDocument
{
    [JsonRequired] public int SchemaVersion { get; init; } = 1;
    [JsonRequired] public List<GameProfile> Profiles { get; init; } = [];
}
