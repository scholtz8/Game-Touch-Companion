namespace GameTouchCompanion.Core;

public static class UrlPolicy
{
    public static bool IsAllowed(string? value) => BrowserUrlPolicy.IsAllowed(value);
}
