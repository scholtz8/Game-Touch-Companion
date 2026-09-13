namespace GameTouchCompanion.Core;

/// <summary>
/// Navigation is limited to explicit HTTP(S) URLs. The reserved local origin exposes only the test pages.
/// </summary>
public static class BrowserUrlPolicy
{
    public const string LocalHomeUrl = "https://touch-test.local/index.html";
    private const string LocalHost = "touch-test.local";

    public static bool IsAllowed(string? value) => TryNormalize(value, out _);

    public static bool TryNormalize(string? input, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var value = input.Trim();
        if ((!value.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
             !value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)) ||
            value.Any(character => char.IsControl(character) || char.IsWhiteSpace(character) || character == '\\') ||
            !HasValidPercentEncoding(value) ||
            !Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            string.IsNullOrWhiteSpace(uri.Host) ||
            uri.HostNameType == UriHostNameType.Unknown ||
            uri.UserInfo.Length != 0)
        {
            return false;
        }

        // Reject even an empty user-info marker (https://@example.com), which Uri.UserInfo omits.
        var authorityStart = value.IndexOf("://", StringComparison.Ordinal) + 3;
        var authorityEnd = value.IndexOfAny(['/', '?', '#'], authorityStart);
        var authority = authorityEnd < 0 ? value[authorityStart..] : value[authorityStart..authorityEnd];
        if (authority.Contains('@', StringComparison.Ordinal))
        {
            return false;
        }

        // A trailing dot is an alias in DNS, but it is not the configured WebView2 virtual host.
        if (uri.IdnHost.TrimEnd('.').Equals(LocalHost, StringComparison.OrdinalIgnoreCase) &&
            (!uri.IdnHost.Equals(LocalHost, StringComparison.OrdinalIgnoreCase) ||
             uri.Scheme != Uri.UriSchemeHttps ||
             !uri.IsDefaultPort ||
             (uri.AbsolutePath != "/index.html" && uri.AbsolutePath != "/second.html")))
        {
            return false;
        }

        normalized = uri.AbsoluteUri;
        return true;
    }

    private static bool HasValidPercentEncoding(string value)
    {
        for (var index = 0; index < value.Length; index++)
        {
            if (value[index] != '%')
            {
                continue;
            }

            if (index + 2 >= value.Length || !Uri.IsHexDigit(value[index + 1]) || !Uri.IsHexDigit(value[index + 2]))
            {
                return false;
            }

            index += 2;
        }

        return true;
    }
}
