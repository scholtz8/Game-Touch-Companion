using GameTouchCompanion.Core;

namespace GameTouchCompanion.Core.Tests;

public sealed class BrowserUrlPolicyTests
{
    [Theory]
    [InlineData("https://example.com", "https://example.com/")]
    [InlineData("  HTTPS://EXAMPLE.COM:443/path?q=test#section  ", "https://example.com/path?q=test#section")]
    [InlineData("http://localhost:8080/wiki", "http://localhost:8080/wiki")]
    [InlineData("https://example.com/search?q=touch%20screen", "https://example.com/search?q=touch%20screen")]
    [InlineData("https://touch-test.local/index.html", BrowserUrlPolicy.LocalHomeUrl)]
    [InlineData("https://touch-test.local/%69ndex.html", BrowserUrlPolicy.LocalHomeUrl)]
    [InlineData("https://touch-test.local:443/second.html?from=home#bottom", "https://touch-test.local/second.html?from=home#bottom")]
    [InlineData(BrowserUrlPolicy.BlankPageUrl, BrowserUrlPolicy.BlankPageUrl)]
    [InlineData("http://[::1]:8080/test", "http://[::1]:8080/test")]
    [InlineData("example.com", "https://example.com/")]
    [InlineData("www.google.com", "https://www.google.com/")]
    [InlineData("google.com/maps?q=test", "https://google.com/maps?q=test")]
    [InlineData("EXAMPLE.COM:8443/path", "https://example.com:8443/path")]
    [InlineData("localhost", "http://localhost/")]
    [InlineData("localhost:8080", "http://localhost:8080/")]
    [InlineData("localhost:3000/app?q=1", "http://localhost:3000/app?q=1")]
    [InlineData("127.0.0.1", "http://127.0.0.1/")]
    [InlineData("127.0.0.1:5000/test", "http://127.0.0.1:5000/test")]
    [InlineData("192.168.1.50:8123/dashboard", "http://192.168.1.50:8123/dashboard")]
    [InlineData("[::1]", "http://[::1]/")]
    [InlineData("[::1]:5000/test", "http://[::1]:5000/test")]
    [InlineData("equipo-local", "http://equipo-local/")]
    [InlineData("equipo-local:8080/wiki", "http://equipo-local:8080/wiki")]
    [InlineData("nas.local/media", "http://nas.local/media")]
    [InlineData("router.lan", "http://router.lan/")]
    [InlineData("service.internal:8080/api", "http://service.internal:8080/api")]
    [InlineData("touch-test.local/index.html", BrowserUrlPolicy.LocalHomeUrl)]
    [InlineData("about:blank", BrowserUrlPolicy.AboutBlankUrl)]
    [InlineData("ABOUT:BLANK", BrowserUrlPolicy.AboutBlankUrl)]
    public void AllowedAddressesAreNormalized(string input, string expected)
    {
        Assert.True(BrowserUrlPolicy.TryNormalize(input, out var normalized));
        Assert.Equal(expected, normalized);
        Assert.True(BrowserUrlPolicy.IsAllowed(normalized));
        Assert.True(UrlPolicy.IsAllowed(normalized));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("//example.com/index.html")]
    [InlineData("/index.html")]
    [InlineData("file:///C:/private/settings.json")]
    [InlineData("C:\\private\\settings.json")]
    [InlineData("C:/private/settings.json")]
    [InlineData("\\\\server\\private\\settings.json")]
    [InlineData("javascript:alert(1)")]
    [InlineData("vbscript:msgbox(1)")]
    [InlineData("data:text/html,hello")]
    [InlineData("mailto:person@example.com")]
    [InlineData("tel:+56212345678")]
    [InlineData("steam://run/123")]
    [InlineData("ftp://example.com/file")]
    [InlineData("edge://settings")]
    [InlineData("about:config")]
    [InlineData("https:")]
    [InlineData("https:/example.com")]
    [InlineData("https:///example.com")]
    [InlineData("http:example.com")]
    [InlineData("https://exa mple.com/")]
    [InlineData("https://example.com/a b")]
    [InlineData("example.com/a b")]
    [InlineData("https://example.com/\nhello")]
    [InlineData("https://example.com\\@evil.example/")]
    [InlineData("https://example.com:99999/")]
    [InlineData("example.com:99999/")]
    [InlineData("localhost:99999")]
    [InlineData("https://example.com/%ZZ")]
    [InlineData("example.com/%ZZ")]
    [InlineData("https://example.com/%2")]
    [InlineData("https://example.com/%")]
    [InlineData("https://user:password@example.com/")]
    [InlineData("user:password@example.com")]
    [InlineData("https://user@example.com/")]
    [InlineData("https://@example.com/")]
    [InlineData("touch-test.local")]
    [InlineData("touch-test.local/private.json")]
    [InlineData("https://touch-test.local/private.json")]
    [InlineData("https://touch-test.local/")]
    [InlineData("https://touch-test.local/index.html/other")]
    [InlineData("https://touch-test.local/index.html/../private.json")]
    [InlineData("https://touch-test.local:444/index.html")]
    [InlineData("http://touch-test.local/index.html")]
    [InlineData("http://touch-test.local:443/index.html")]
    [InlineData("https://touch-test.local./index.html")]
    public void UnsafeOrMalformedAddressesAreRejected(string? input)
    {
        Assert.False(BrowserUrlPolicy.TryNormalize(input, out var normalized));
        Assert.Empty(normalized);
        Assert.False(BrowserUrlPolicy.IsAllowed(input));
        Assert.False(UrlPolicy.IsAllowed(input));
    }
}
