using GameTouchCompanion.Native;

namespace GameTouchCompanion.Native.Tests;

public sealed class NoActivateWindowServiceTests
{
    [Fact]
    public void ComposeStyleAddsNoActivateAndToolWindow()
    {
        var result = NoActivateWindowService.ComposeExtendedStyle((nint)0x20);
        Assert.Equal((long)0x080000A0, result.ToInt64());
    }

    [Fact]
    public void MouseActivateReturnsNoActivate()
    {
        Assert.True(NoActivateWindowService.TryHandleMessage(NativeConstants.WmMouseActivate, out var result));
        Assert.Equal((nint)NativeConstants.MaNoActivate, result);
    }

    [Fact]
    public void OtherMessageIsNotHandled() =>
        Assert.False(NoActivateWindowService.TryHandleMessage(0x1234, out _));
}
