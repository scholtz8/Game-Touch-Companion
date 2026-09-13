using GameTouchCompanion.Core;

namespace GameTouchCompanion.Native;

internal static class MonitorProfileMapper
{
    internal static DisplayRect ToDisplayRect(NativeRect rectangle) => new(
        rectangle.Left,
        rectangle.Top,
        checked(rectangle.Right - rectangle.Left),
        checked(rectangle.Bottom - rectangle.Top));

    internal static MonitorProfile ToMonitorProfile(MonitorSnapshot monitor) => new(
        monitor.DeviceName,
        ToDisplayRect(monitor.Bounds),
        ToDisplayRect(monitor.WorkingArea),
        (monitor.Flags & NativeConstants.MonitorInfoPrimary) != 0);

    internal static IReadOnlyList<MonitorProfile> Order(IEnumerable<MonitorProfile> monitors)
    {
        ArgumentNullException.ThrowIfNull(monitors);

        return monitors
            .OrderByDescending(static monitor => monitor.IsPrimary)
            .ThenBy(static monitor => monitor.Bounds.X)
            .ThenBy(static monitor => monitor.Bounds.Y)
            .ThenBy(static monitor => monitor.DeviceName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}

internal readonly record struct MonitorSnapshot(
    string DeviceName,
    NativeRect Bounds,
    NativeRect WorkingArea,
    uint Flags);
