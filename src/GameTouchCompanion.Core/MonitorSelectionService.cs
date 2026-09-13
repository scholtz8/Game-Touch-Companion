namespace GameTouchCompanion.Core;

public enum MonitorSelectionIssueCode
{
    GameMonitorUnavailable,
    CompanionMonitorUnavailable,
    SameMonitorSelectionBlocked,
    SameMonitorOverrideEnabled,
    SingleMonitorAvailable,
}

public enum MonitorSelectionIssueSeverity
{
    Warning,
    Error,
}

public sealed record MonitorSelectionIssue(
    MonitorSelectionIssueCode Code,
    MonitorSelectionIssueSeverity Severity,
    string Message)
{
    public FormattableString? FormattedMessage { get; init; }
    public static MonitorSelectionIssue FromFormat(MonitorSelectionIssueCode code, MonitorSelectionIssueSeverity severity, FormattableString message) =>
        new(code, severity, message.ToString()) { FormattedMessage = message };
}

public sealed record MonitorSelectionValidation(
    bool IsValid,
    IReadOnlyList<MonitorSelectionIssue> Issues);

public sealed record ResolvedMonitorSelection(
    MonitorProfile GameMonitor,
    MonitorProfile CompanionMonitor,
    IReadOnlyList<MonitorSelectionIssue> Issues)
{
    public bool UsesSameMonitor => MonitorSelectionService.DeviceNamesEqual(
        GameMonitor.DeviceName,
        CompanionMonitor.DeviceName);
}

/// <summary>
/// Applies monitor-selection policy independently from monitor enumeration and UI concerns.
/// </summary>
public sealed class MonitorSelectionService
{
    private static readonly StringComparer DeviceNameComparer = StringComparer.OrdinalIgnoreCase;

    public ResolvedMonitorSelection Resolve(
        IReadOnlyList<MonitorProfile> availableMonitors,
        ApplicationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(availableMonitors);
        ArgumentNullException.ThrowIfNull(settings);

        var monitors = ValidateAndCopyMonitors(availableMonitors);
        if (monitors.Count == 0)
        {
            throw new InvalidOperationException("No monitors are available.");
        }

        var issues = new List<MonitorSelectionIssue>();
        var gameMonitor = FindByDeviceName(monitors, settings.GameMonitorDeviceName);
        if (gameMonitor is null)
        {
            gameMonitor = monitors.FirstOrDefault(static monitor => monitor.IsPrimary) ?? monitors[0];
            AddUnavailableIssueIfConfigured(
                issues,
                settings.GameMonitorDeviceName,
                MonitorSelectionIssueCode.GameMonitorUnavailable,
                "game",
                gameMonitor.DeviceName);
        }

        var companionMonitor = FindByDeviceName(monitors, settings.CompanionMonitorDeviceName);
        if (companionMonitor is null)
        {
            companionMonitor = FindFirstDifferentMonitor(monitors, gameMonitor) ?? gameMonitor;
            AddUnavailableIssueIfConfigured(
                issues,
                settings.CompanionMonitorDeviceName,
                MonitorSelectionIssueCode.CompanionMonitorUnavailable,
                "Companion",
                companionMonitor.DeviceName);
        }

        if (DeviceNamesEqual(gameMonitor.DeviceName, companionMonitor.DeviceName))
        {
            var alternateCompanion = FindFirstDifferentMonitor(monitors, gameMonitor);
            if (alternateCompanion is null)
            {
                issues.Add(new MonitorSelectionIssue(
                    MonitorSelectionIssueCode.SingleMonitorAvailable,
                    MonitorSelectionIssueSeverity.Warning,
                    "Only one monitor is available; the game and Companion must share it."));
            }
            else if (settings.AllowSameMonitorForTesting)
            {
                issues.Add(new MonitorSelectionIssue(
                    MonitorSelectionIssueCode.SameMonitorOverrideEnabled,
                    MonitorSelectionIssueSeverity.Warning,
                    "The game and Companion are using the same monitor because the testing override is enabled."));
            }
            else
            {
                companionMonitor = alternateCompanion;
                issues.Add(MonitorSelectionIssue.FromFormat(
                    MonitorSelectionIssueCode.SameMonitorSelectionBlocked,
                    MonitorSelectionIssueSeverity.Warning,
                    $"The duplicate monitor selection was blocked; Companion will use {companionMonitor.DeviceName}."));
            }
        }

        return new ResolvedMonitorSelection(gameMonitor, companionMonitor, issues.AsReadOnly());
    }

    public MonitorSelectionValidation ValidateSelection(
        MonitorProfile gameMonitor,
        MonitorProfile companionMonitor,
        IReadOnlyList<MonitorProfile> availableMonitors,
        bool allowSameMonitorForTesting)
    {
        ArgumentNullException.ThrowIfNull(gameMonitor);
        ArgumentNullException.ThrowIfNull(companionMonitor);
        ArgumentNullException.ThrowIfNull(availableMonitors);

        var monitors = ValidateAndCopyMonitors(availableMonitors);
        var issues = new List<MonitorSelectionIssue>();

        if (FindByDeviceName(monitors, gameMonitor.DeviceName) is null)
        {
            issues.Add(MonitorSelectionIssue.FromFormat(
                MonitorSelectionIssueCode.GameMonitorUnavailable,
                MonitorSelectionIssueSeverity.Error,
                $"The selected game monitor {gameMonitor.DeviceName} is not available."));
        }

        if (FindByDeviceName(monitors, companionMonitor.DeviceName) is null)
        {
            issues.Add(MonitorSelectionIssue.FromFormat(
                MonitorSelectionIssueCode.CompanionMonitorUnavailable,
                MonitorSelectionIssueSeverity.Error,
                $"The selected Companion monitor {companionMonitor.DeviceName} is not available."));
        }

        if (DeviceNamesEqual(gameMonitor.DeviceName, companionMonitor.DeviceName))
        {
            var distinctMonitorCount = monitors
                .Select(static monitor => monitor.DeviceName)
                .Distinct(DeviceNameComparer)
                .Count();

            if (distinctMonitorCount <= 1)
            {
                issues.Add(new MonitorSelectionIssue(
                    MonitorSelectionIssueCode.SingleMonitorAvailable,
                    MonitorSelectionIssueSeverity.Warning,
                    "Only one monitor is available; the game and Companion must share it."));
            }
            else if (allowSameMonitorForTesting)
            {
                issues.Add(new MonitorSelectionIssue(
                    MonitorSelectionIssueCode.SameMonitorOverrideEnabled,
                    MonitorSelectionIssueSeverity.Warning,
                    "The game and Companion will use the same monitor because the testing override is enabled."));
            }
            else
            {
                issues.Add(new MonitorSelectionIssue(
                    MonitorSelectionIssueCode.SameMonitorSelectionBlocked,
                    MonitorSelectionIssueSeverity.Error,
                    "Select different game and Companion monitors, or explicitly enable the testing override."));
            }
        }

        return new MonitorSelectionValidation(
            issues.All(static issue => issue.Severity != MonitorSelectionIssueSeverity.Error),
            issues.AsReadOnly());
    }

    internal static bool DeviceNamesEqual(string left, string right) =>
        DeviceNameComparer.Equals(left, right);

    private static List<MonitorProfile> ValidateAndCopyMonitors(
        IReadOnlyList<MonitorProfile> availableMonitors)
    {
        var monitors = new List<MonitorProfile>(availableMonitors.Count);
        var deviceNames = new HashSet<string>(DeviceNameComparer);

        foreach (var monitor in availableMonitors)
        {
            if (monitor is null)
            {
                throw new ArgumentException("The monitor list cannot contain null entries.", nameof(availableMonitors));
            }

            if (string.IsNullOrWhiteSpace(monitor.DeviceName))
            {
                throw new ArgumentException("Every monitor must have a device name.", nameof(availableMonitors));
            }

            if (!deviceNames.Add(monitor.DeviceName))
            {
                throw new ArgumentException(
                    $"The monitor list contains the duplicate device name {monitor.DeviceName}.",
                    nameof(availableMonitors));
            }

            monitors.Add(monitor);
        }

        return monitors;
    }

    private static MonitorProfile? FindByDeviceName(
        IReadOnlyList<MonitorProfile> monitors,
        string? deviceName)
    {
        if (string.IsNullOrWhiteSpace(deviceName))
        {
            return null;
        }

        return monitors.FirstOrDefault(monitor => DeviceNameComparer.Equals(monitor.DeviceName, deviceName));
    }

    private static MonitorProfile? FindFirstDifferentMonitor(
        IReadOnlyList<MonitorProfile> monitors,
        MonitorProfile selectedMonitor) =>
        monitors.FirstOrDefault(monitor =>
            !DeviceNameComparer.Equals(monitor.DeviceName, selectedMonitor.DeviceName));

    private static void AddUnavailableIssueIfConfigured(
        ICollection<MonitorSelectionIssue> issues,
        string? configuredDeviceName,
        MonitorSelectionIssueCode code,
        string role,
        string fallbackDeviceName)
    {
        if (!string.IsNullOrWhiteSpace(configuredDeviceName))
        {
            issues.Add(MonitorSelectionIssue.FromFormat(
                code,
                MonitorSelectionIssueSeverity.Warning,
                $"The configured {role} monitor {configuredDeviceName} is unavailable; using {fallbackDeviceName}."));
        }
    }
}
