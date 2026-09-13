namespace GameTouchCompanion.Core;

public sealed record ApplicationSettings
{
    public string? GameMonitorDeviceName { get; init; }

    public string? CompanionMonitorDeviceName { get; init; }

    /// <summary>
    /// Allows both roles to use one monitor. This is intended only for explicit testing scenarios.
    /// </summary>
    public bool AllowSameMonitorForTesting { get; init; }

    public bool EnableDetectionOnStartup { get; init; }
    public bool StartMinimizedToTray { get; init; }
    public bool CloseToTray { get; init; }
}
