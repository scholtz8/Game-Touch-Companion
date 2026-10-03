using System.Text.Json.Serialization;

namespace GameTouchCompanion.Core;

public sealed record BrowserFavorite(string Title, string Url)
{
    [JsonIgnore]
    public string DisplayText => string.IsNullOrWhiteSpace(Title) ? Url : Title.Trim();
}
