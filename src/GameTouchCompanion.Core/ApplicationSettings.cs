namespace GameTouchCompanion.Core;

public sealed record ApplicationSettings
{
    /// <summary>
    /// Persistent monitor identity. This is preferred over the legacy GDI device name.
    /// </summary>
    public string? GameMonitorId { get; init; }

    /// <summary>
    /// Persistent monitor identity. This is preferred over the legacy GDI device name.
    /// </summary>
    public string? CompanionMonitorId { get; init; }

    /// <summary>
    /// Legacy/current-session GDI name kept for migration and diagnostics only.
    /// </summary>
    public string? GameMonitorDeviceName { get; init; }

    /// <summary>
    /// Legacy/current-session GDI name kept for migration and diagnostics only.
    /// </summary>
    public string? CompanionMonitorDeviceName { get; init; }

    /// <summary>
    /// Allows both roles to use one monitor. This is intended only for explicit testing scenarios.
    /// </summary>
    public bool AllowSameMonitorForTesting { get; init; }

    public bool StartMinimizedToTray { get; init; }
    public bool CloseToTray { get; init; }
}
