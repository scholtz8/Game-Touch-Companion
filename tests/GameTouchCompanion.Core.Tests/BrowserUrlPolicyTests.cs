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
    [InlineData("http://[::1]:8080/test", "http://[::1]:8080/test")]
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
    [InlineData("example.com")]
    [InlineData("//example.com/index.html")]
    [InlineData("/index.html")]
    [InlineData("file:///C:/private/settings.json")]
    [InlineData("C:\\private\\settings.json")]
    [InlineData("\\\\server\\private\\settings.json")]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,hello")]
    [InlineData("mailto:person@example.com")]
    [InlineData("ftp://example.com/file")]
    [InlineData("edge://settings")]
    [InlineData("https:")]
    [InlineData("https:///example.com")]
    [InlineData("http:example.com")]
    [InlineData("https://exa mple.com/")]
    [InlineData("https://example.com/a b")]
    [InlineData("https://example.com/\nhello")]
    [InlineData("https://example.com\\@evil.example/")]
    [InlineData("https://example.com:99999/")]
    [InlineData("https://example.com/%ZZ")]
    [InlineData("https://example.com/%2")]
    [InlineData("https://example.com/%")]
    [InlineData("https://user:password@example.com/")]
    [InlineData("https://user@example.com/")]
    [InlineData("https://@example.com/")]
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
