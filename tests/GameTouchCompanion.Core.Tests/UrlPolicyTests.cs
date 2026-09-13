using GameTouchCompanion.Core;

namespace GameTouchCompanion.Core.Tests;

public sealed class UrlPolicyTests
{
    [Theory]
    [InlineData("https://example.com")]
    [InlineData("http://localhost/test")]
    public void HttpAndHttpsUrlsAreAllowed(string value) => Assert.True(UrlPolicy.IsAllowed(value));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("file:///c:/secret.txt")]
    [InlineData("javascript:alert(1)")]
    public void UnsafeOrInvalidUrlsAreRejected(string? value) => Assert.False(UrlPolicy.IsAllowed(value));
}
