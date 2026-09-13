using GameTouchCompanion.Core;

namespace GameTouchCompanion.Core.Tests;

public sealed class FocusStatusTests
{
    [Fact]
    public void ReportsExpectedWindowAsForeground() =>
        Assert.True(new FocusStatus((nint)42, (nint)42, 0).IsExpectedForeground);

    [Fact]
    public void ZeroHandleIsNeverExpectedForeground() =>
        Assert.False(new FocusStatus(nint.Zero, nint.Zero, 0).IsExpectedForeground);
}
