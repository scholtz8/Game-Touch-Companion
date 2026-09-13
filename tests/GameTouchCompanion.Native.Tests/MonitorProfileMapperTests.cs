using GameTouchCompanion.Core;

namespace GameTouchCompanion.Native.Tests;

public sealed class MonitorProfileMapperTests
{
    [Fact]
    public void ToDisplayRectPreservesNegativeCoordinatesAndCalculatesSize()
    {
        var nativeRectangle = new NativeRect(-2560, -200, 0, 1240);

        var result = MonitorProfileMapper.ToDisplayRect(nativeRectangle);

        Assert.Equal(new DisplayRect(-2560, -200, 2560, 1440), result);
    }

    [Fact]
    public void ToMonitorProfileMapsAllMonitorInfoFields()
    {
        var snapshot = new MonitorSnapshot(
            @"\\.\DISPLAY2",
            new NativeRect(-1920, 0, 0, 1080),
            new NativeRect(-1920, 0, 0, 1040),
            NativeConstants.MonitorInfoPrimary);

        var result = MonitorProfileMapper.ToMonitorProfile(snapshot);

        Assert.Equal(@"\\.\DISPLAY2", result.DeviceName);
        Assert.Equal(new DisplayRect(-1920, 0, 1920, 1080), result.Bounds);
        Assert.Equal(new DisplayRect(-1920, 0, 1920, 1040), result.WorkingArea);
        Assert.True(result.IsPrimary);
    }

    [Fact]
    public void OrderPlacesPrimaryFirstThenUsesCoordinatesAndDeviceName()
    {
        var monitors = new[]
        {
            CreateMonitor(@"\\.\DISPLAY4", 0, 1080, isPrimary: false),
            CreateMonitor(@"\\.\DISPLAY3", -1920, 0, isPrimary: false),
            CreateMonitor(@"\\.\DISPLAY2", 0, -1080, isPrimary: false),
            CreateMonitor(@"\\.\DISPLAY1", 0, 0, isPrimary: true),
            CreateMonitor(@"\\.\DISPLAY5", 0, 1080, isPrimary: false),
        };

        var result = MonitorProfileMapper.Order(monitors);

        Assert.Equal(
            [@"\\.\DISPLAY1", @"\\.\DISPLAY3", @"\\.\DISPLAY2", @"\\.\DISPLAY4", @"\\.\DISPLAY5"],
            result.Select(static monitor => monitor.DeviceName));
    }

    private static MonitorProfile CreateMonitor(string deviceName, int x, int y, bool isPrimary)
    {
        var bounds = new DisplayRect(x, y, 1920, 1080);
        return new MonitorProfile(deviceName, bounds, bounds, isPrimary);
    }
}
