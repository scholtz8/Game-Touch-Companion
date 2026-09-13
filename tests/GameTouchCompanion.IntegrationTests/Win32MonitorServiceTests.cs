using GameTouchCompanion.Native;

namespace GameTouchCompanion.IntegrationTests;

public sealed class Win32MonitorServiceTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public void EnumeratesTheCurrentWindowsDesktop()
    {
        if (!OperatingSystem.IsWindows()) return;

        var monitors = new Win32MonitorService().GetMonitors();

        Assert.NotEmpty(monitors);
        Assert.Single(monitors, static monitor => monitor.IsPrimary);
        Assert.All(monitors, static monitor =>
        {
            Assert.False(string.IsNullOrWhiteSpace(monitor.DeviceName));
            Assert.True(monitor.Bounds.Width > 0);
            Assert.True(monitor.Bounds.Height > 0);
        });
    }
}
