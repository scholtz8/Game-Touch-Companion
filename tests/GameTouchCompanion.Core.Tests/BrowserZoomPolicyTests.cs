using GameTouchCompanion.Core;

namespace GameTouchCompanion.Core.Tests;

public sealed class BrowserZoomPolicyTests
{
    [Theory]
    [InlineData(50)]
    [InlineData(100)]
    [InlineData(200)]
    public void ValidZoomRangeUsesTenPercentSteps(int percent) =>
        Assert.True(BrowserZoomPolicy.IsValidPercent(percent));

    [Theory]
    [InlineData(40)]
    [InlineData(55)]
    [InlineData(210)]
    public void InvalidZoomValuesAreRejected(int percent) =>
        Assert.False(BrowserZoomPolicy.IsValidPercent(percent));

    [Theory]
    [InlineData(1, 50)]
    [InlineData(54, 50)]
    [InlineData(56, 60)]
    [InlineData(147, 150)]
    [InlineData(205, 200)]
    public void NormalizeZoomClampsAndRoundsToTenPercentSteps(int input, int expected) =>
        Assert.Equal(expected, BrowserZoomPolicy.NormalizePercent(input));

    [Theory]
    [InlineData("https://EXAMPLE.com/wiki", "example.com")]
    [InlineData("http://localhost:8080/page", "localhost")]
    [InlineData("https://127.0.0.1/test", "127.0.0.1")]
    public void HostIsNormalizedFromAllowedBrowserUrl(string url, string expected) =>
        Assert.True(BrowserZoomPolicy.TryGetHost(url, out var host) && host == expected);
}
