using GameTouchCompanion.Core;

namespace GameTouchCompanion.Native.Tests;

public sealed class Win32WindowPlacementServiceTests
{
    [Fact]
    public void CreatePlacementUsesPhysicalMonitorBoundsWithoutClamping()
    {
        var result = Win32WindowPlacementService.CreatePlacement(
            new DisplayRect(-2560, -1440, 2560, 1440));

        Assert.Equal(-2560, result.X);
        Assert.Equal(-1440, result.Y);
        Assert.Equal(2560, result.Width);
        Assert.Equal(1440, result.Height);
    }

    [Fact]
    public void CreatePlacementPreventsActivationAndPreservesZOrder()
    {
        var result = Win32WindowPlacementService.CreatePlacement(
            new DisplayRect(1920, 0, 3840, 2160));

        Assert.Equal(
            NativeConstants.SwpNoActivate | NativeConstants.SwpNoZOrder,
            result.Flags);
        Assert.Equal(0u, result.Flags & NativeConstants.SwpNoMove);
        Assert.Equal(0u, result.Flags & NativeConstants.SwpNoSize);
    }

    [Theory]
    [InlineData(0, 1080)]
    [InlineData(1920, 0)]
    [InlineData(-1, 1080)]
    [InlineData(1920, -1)]
    public void CreatePlacementRejectsNonPositiveDimensions(int width, int height)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Win32WindowPlacementService.CreatePlacement(new DisplayRect(0, 0, width, height)));
    }

    [Fact]
    public void PlaceOnMonitorRejectsNullWindowHandleBeforeCallingWin32()
    {
        var bounds = new DisplayRect(0, 0, 1920, 1080);
        var monitor = new MonitorProfile(@"\\.\DISPLAY1", bounds, bounds, true);

        Assert.Throws<ArgumentException>(() =>
            Win32WindowPlacementService.PlaceOnMonitor(nint.Zero, monitor));
    }
}
