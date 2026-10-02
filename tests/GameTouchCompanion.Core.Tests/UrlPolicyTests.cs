using GameTouchCompanion.Core;

namespace GameTouchCompanion.Core.Tests;

public sealed class UrlPolicyTests
{
    [Theory]
    [InlineData("https://example.com")]
    [InlineData("http://localhost/test")]
    [InlineData("example.com")]
    [InlineData("localhost:8080")]
    [InlineData("192.168.1.20:3000")]
    [InlineData("about:blank")]
    public void SupportedUrlsAndUserFriendlyAddressesAreAllowed(string value) => Assert.True(UrlPolicy.IsAllowed(value));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("file:///c:/secret.txt")]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,hello")]
    [InlineData("mailto:person@example.com")]
    public void UnsafeOrInvalidUrlsAreRejected(string? value) => Assert.False(UrlPolicy.IsAllowed(value));
}
