using System.Net;

namespace GameTouchCompanion.Core;

/// <summary>
/// Normalizes user-friendly browser addresses while keeping Companion navigation on a small,
/// explicit set of safe schemes. Public hosts default to HTTPS; local hosts and IP addresses
/// default to HTTP for compatibility with common LAN/development services.
/// </summary>
public static class BrowserUrlPolicy
{
    public const string LocalHomeUrl = "https://touch-test.local/index.html";
    public const string BlankPageUrl = "https://touch-test.local/blank.html";
    public const string AboutBlankUrl = "about:blank";
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
        if (value.Any(character => char.IsControl(character) || char.IsWhiteSpace(character) || character == '\\') ||
            !HasValidPercentEncoding(value))
        {
            return false;
        }

        if (value.Equals(AboutBlankUrl, StringComparison.OrdinalIgnoreCase))
        {
            normalized = AboutBlankUrl;
            return true;
        }

        // Protocol-relative addresses are intentionally not guessed; users can omit the scheme entirely instead.
        if (value.StartsWith("//", StringComparison.Ordinal))
        {
            return false;
        }

        string candidate;
        if (HasExplicitScheme(value, out var scheme))
        {
            if (!scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
                !scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Do not accept malformed forms such as http:example.com or https:/example.com.
            if (!value.StartsWith($"{scheme}://", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            candidate = value;
        }
        else
        {
            if (!TryBuildImplicitAddress(value, out candidate))
            {
                return false;
            }
        }

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            string.IsNullOrWhiteSpace(uri.Host) ||
            uri.HostNameType == UriHostNameType.Unknown ||
            uri.UserInfo.Length != 0 ||
            !HasValidPort(uri))
        {
            return false;
        }

        if (RawAuthorityContainsUserInfo(candidate))
        {
            return false;
        }

        // A trailing dot is an alias in DNS, but it is not the configured WebView2 virtual host.
        if (uri.IdnHost.TrimEnd('.').Equals(LocalHost, StringComparison.OrdinalIgnoreCase) &&
            (!uri.IdnHost.Equals(LocalHost, StringComparison.OrdinalIgnoreCase) ||
             uri.Scheme != Uri.UriSchemeHttps ||
             !uri.IsDefaultPort ||
             (uri.AbsolutePath != "/index.html" && uri.AbsolutePath != "/second.html" && uri.AbsolutePath != "/blank.html")))
        {
            return false;
        }

        normalized = uri.AbsoluteUri;
        return true;
    }

    private static bool TryBuildImplicitAddress(string value, out string candidate)
    {
        candidate = string.Empty;

        // Parse once with HTTP only to identify the host. The final scheme is chosen below.
        if (!Uri.TryCreate("http://" + value, UriKind.Absolute, out var probe) ||
            string.IsNullOrWhiteSpace(probe.Host) ||
            probe.HostNameType == UriHostNameType.Unknown ||
            probe.UserInfo.Length != 0 ||
            !HasValidPort(probe) ||
            RawAuthorityContainsUserInfo("http://" + value))
        {
            return false;
        }

        var scheme = ShouldDefaultToHttp(probe) ? Uri.UriSchemeHttp : Uri.UriSchemeHttps;
        candidate = $"{scheme}://{value}";
        return true;
    }

    private static bool ShouldDefaultToHttp(Uri uri)
    {
        if (uri.IdnHost.Equals(LocalHost, StringComparison.OrdinalIgnoreCase))
        {
            // The packaged test pages are exposed only through the HTTPS WebView2 virtual host.
            return false;
        }

        if (uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            IPAddress.TryParse(uri.Host.Trim('[', ']'), out _))
        {
            return true;
        }

        // Single-label hosts are normally LAN/dev machine names rather than public DNS names.
        if (!uri.IdnHost.Contains('.'))
        {
            return true;
        }

        // Common private/mDNS suffixes are frequently served without TLS.
        return uri.IdnHost.EndsWith(".local", StringComparison.OrdinalIgnoreCase) ||
               uri.IdnHost.EndsWith(".lan", StringComparison.OrdinalIgnoreCase) ||
               uri.IdnHost.EndsWith(".home", StringComparison.OrdinalIgnoreCase) ||
               uri.IdnHost.EndsWith(".internal", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasExplicitScheme(string value, out string scheme)
    {
        scheme = string.Empty;
        var colon = value.IndexOf(':');
        if (colon <= 0)
        {
            return false;
        }

        var firstDelimiter = value.IndexOfAny(['/', '?', '#']);
        if (firstDelimiter >= 0 && colon > firstDelimiter)
        {
            return false;
        }

        var prefix = value[..colon];
        if (!char.IsLetter(prefix[0]) || prefix.Skip(1).Any(character => !char.IsLetterOrDigit(character) && character != '+' && character != '-' && character != '.'))
        {
            return false;
        }

        // A host followed by a numeric port (localhost:8080, example.com:8443,
        // service.internal:8080) is an address without a scheme, not a custom URL scheme.
        var remainder = value[(colon + 1)..];
        var portEnd = remainder.IndexOfAny(['/', '?', '#']);
        var portText = portEnd < 0 ? remainder : remainder[..portEnd];
        if (portText.Length > 0 &&
            portText.All(char.IsDigit) &&
            int.TryParse(portText, out var port) &&
            port is > 0 and <= 65535 &&
            !prefix.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
            !prefix.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        scheme = prefix;
        return true;
    }

    private static bool HasValidPort(Uri uri)
    {
        try
        {
            _ = uri.Port;
            return true;
        }
        catch (UriFormatException)
        {
            return false;
        }
    }

    private static bool RawAuthorityContainsUserInfo(string value)
    {
        var authorityStart = value.IndexOf("://", StringComparison.Ordinal);
        if (authorityStart < 0)
        {
            return false;
        }

        authorityStart += 3;
        var authorityEnd = value.IndexOfAny(['/', '?', '#'], authorityStart);
        var authority = authorityEnd < 0 ? value[authorityStart..] : value[authorityStart..authorityEnd];
        return authority.Contains('@', StringComparison.Ordinal);
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
