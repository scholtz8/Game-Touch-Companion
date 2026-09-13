using GameTouchCompanion.Core;

namespace GameTouchCompanion.Core.Tests;

public sealed class MonitorSelectionServiceTests
{
    private readonly MonitorSelectionService service = new();

    [Fact]
    public void MissingConfiguredMonitorsFallBackToPrimaryAndAnotherMonitor()
    {
        var primary = CreateMonitor("\\\\.\\DISPLAY1", 0, 0, isPrimary: true);
        var secondary = CreateMonitor("\\\\.\\DISPLAY2", -1920, 0);
        var settings = new ApplicationSettings
        {
            GameMonitorDeviceName = "\\\\.\\REMOVED_GAME",
            CompanionMonitorDeviceName = "\\\\.\\REMOVED_COMPANION",
        };

        var result = service.Resolve([secondary, primary], settings);

        Assert.Equal(primary, result.GameMonitor);
        Assert.Equal(secondary, result.CompanionMonitor);
        Assert.False(result.UsesSameMonitor);
        Assert.Collection(
            result.Issues,
            issue => Assert.Equal(MonitorSelectionIssueCode.GameMonitorUnavailable, issue.Code),
            issue => Assert.Equal(MonitorSelectionIssueCode.CompanionMonitorUnavailable, issue.Code));
    }

    [Fact]
    public void DuplicateSelectionIsMovedToAnotherMonitorByDefault()
    {
        var primary = CreateMonitor("\\\\.\\DISPLAY1", 0, 0, isPrimary: true);
        var secondary = CreateMonitor("\\\\.\\DISPLAY2", 1920, 0);
        var settings = new ApplicationSettings
        {
            GameMonitorDeviceName = primary.DeviceName,
            CompanionMonitorDeviceName = primary.DeviceName,
        };

        var result = service.Resolve([primary, secondary], settings);

        Assert.Equal(primary, result.GameMonitor);
        Assert.Equal(secondary, result.CompanionMonitor);
        Assert.False(result.UsesSameMonitor);
        var issue = Assert.Single(result.Issues);
        Assert.Equal(MonitorSelectionIssueCode.SameMonitorSelectionBlocked, issue.Code);
        Assert.Equal(MonitorSelectionIssueSeverity.Warning, issue.Severity);
    }

    [Fact]
    public void ExplicitTestingOverrideKeepsDuplicateSelectionAndWarns()
    {
        var primary = CreateMonitor("\\\\.\\DISPLAY1", 0, 0, isPrimary: true);
        var secondary = CreateMonitor("\\\\.\\DISPLAY2", 1920, 0);
        var settings = new ApplicationSettings
        {
            GameMonitorDeviceName = primary.DeviceName,
            CompanionMonitorDeviceName = primary.DeviceName,
            AllowSameMonitorForTesting = true,
        };

        var result = service.Resolve([primary, secondary], settings);

        Assert.Equal(primary, result.GameMonitor);
        Assert.Equal(primary, result.CompanionMonitor);
        Assert.True(result.UsesSameMonitor);
        var issue = Assert.Single(result.Issues);
        Assert.Equal(MonitorSelectionIssueCode.SameMonitorOverrideEnabled, issue.Code);
        Assert.Equal(MonitorSelectionIssueSeverity.Warning, issue.Severity);
    }

    [Fact]
    public void ValidationBlocksDuplicateSelectionUntilOverrideIsExplicit()
    {
        var primary = CreateMonitor("\\\\.\\DISPLAY1", 0, 0, isPrimary: true);
        var secondary = CreateMonitor("\\\\.\\DISPLAY2", 1920, 0);

        var blocked = service.ValidateSelection(primary, primary, [primary, secondary], false);
        var allowed = service.ValidateSelection(primary, primary, [primary, secondary], true);

        Assert.False(blocked.IsValid);
        Assert.Equal(MonitorSelectionIssueSeverity.Error, Assert.Single(blocked.Issues).Severity);
        Assert.True(allowed.IsValid);
        Assert.Equal(
            MonitorSelectionIssueCode.SameMonitorOverrideEnabled,
            Assert.Single(allowed.Issues).Code);
    }

    [Fact]
    public void SingleMonitorIsAValidUnavoidableFallback()
    {
        var onlyMonitor = CreateMonitor("\\\\.\\DISPLAY1", 0, 0, isPrimary: true);

        var result = service.Resolve([onlyMonitor], new ApplicationSettings());
        var validation = service.ValidateSelection(
            onlyMonitor,
            onlyMonitor,
            [onlyMonitor],
            allowSameMonitorForTesting: false);

        Assert.True(result.UsesSameMonitor);
        Assert.Equal(MonitorSelectionIssueCode.SingleMonitorAvailable, Assert.Single(result.Issues).Code);
        Assert.True(validation.IsValid);
        Assert.Equal(MonitorSelectionIssueSeverity.Warning, Assert.Single(validation.Issues).Severity);
    }

    [Fact]
    public void NegativeVirtualDesktopCoordinatesArePreserved()
    {
        var primary = CreateMonitor("\\\\.\\DISPLAY1", 0, 0, isPrimary: true);
        var leftMonitor = CreateMonitor("\\\\.\\DISPLAY2", -1920, -240);
        var settings = new ApplicationSettings
        {
            GameMonitorDeviceName = primary.DeviceName,
            CompanionMonitorDeviceName = leftMonitor.DeviceName,
        };

        var result = service.Resolve([primary, leftMonitor], settings);

        Assert.Equal(-1920, result.CompanionMonitor.Bounds.X);
        Assert.Equal(-240, result.CompanionMonitor.Bounds.Y);
        Assert.Contains("@ (-1920, -240)", result.CompanionMonitor.DisplayLabel, StringComparison.Ordinal);
    }

    [Fact]
    public void DeviceNamesAreMatchedCaseInsensitively()
    {
        var primary = CreateMonitor("\\\\.\\DISPLAY1", 0, 0, isPrimary: true);
        var secondary = CreateMonitor("\\\\.\\DISPLAY2", 1920, 0);
        var settings = new ApplicationSettings
        {
            GameMonitorDeviceName = "\\\\.\\display1",
            CompanionMonitorDeviceName = "\\\\.\\display2",
        };

        var result = service.Resolve([primary, secondary], settings);

        Assert.Equal(primary, result.GameMonitor);
        Assert.Equal(secondary, result.CompanionMonitor);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void ResolveRejectsAnEmptyMonitorList()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => service.Resolve([], new ApplicationSettings()));

        Assert.Equal("No monitors are available.", exception.Message);
    }

    private static MonitorProfile CreateMonitor(
        string deviceName,
        int x,
        int y,
        bool isPrimary = false) =>
        new(
            deviceName,
            new DisplayRect(x, y, 1920, 1080),
            new DisplayRect(x, y, 1920, 1040),
            isPrimary);
}
