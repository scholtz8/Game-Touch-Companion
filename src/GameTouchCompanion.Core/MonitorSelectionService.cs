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
    public bool UsesSameMonitor => MonitorSelectionService.IdentityEquals(GameMonitor, CompanionMonitor);
}

/// <summary>
/// Applies monitor-selection policy independently from monitor enumeration and UI concerns.
/// Persistent monitor ids are preferred. GDI device names are used only for legacy migration.
/// </summary>
public sealed class MonitorSelectionService
{
    private static readonly StringComparer IdentityComparer = StringComparer.OrdinalIgnoreCase;

    public ResolvedMonitorSelection Resolve(
        IReadOnlyList<MonitorProfile> availableMonitors,
        ApplicationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(availableMonitors);
        ArgumentNullException.ThrowIfNull(settings);

        var monitors = ValidateAndCopyMonitors(availableMonitors);
        if (monitors.Count == 0)
            throw new InvalidOperationException("No monitors are available.");

        var issues = new List<MonitorSelectionIssue>();
        var gameMonitor = FindConfiguredMonitor(monitors, settings.GameMonitorId, settings.GameMonitorDeviceName);
        if (gameMonitor is null)
        {
            gameMonitor = monitors.FirstOrDefault(static monitor => monitor.IsPrimary) ?? monitors[0];
            AddUnavailableIssueIfConfigured(
                issues,
                settings.GameMonitorId ?? settings.GameMonitorDeviceName,
                MonitorSelectionIssueCode.GameMonitorUnavailable,
                "game",
                gameMonitor.DisplayLabel);
        }

        var companionMonitor = FindConfiguredMonitor(monitors, settings.CompanionMonitorId, settings.CompanionMonitorDeviceName);
        if (companionMonitor is null)
        {
            companionMonitor = FindFirstDifferentMonitor(monitors, gameMonitor) ?? gameMonitor;
            AddUnavailableIssueIfConfigured(
                issues,
                settings.CompanionMonitorId ?? settings.CompanionMonitorDeviceName,
                MonitorSelectionIssueCode.CompanionMonitorUnavailable,
                "Companion",
                companionMonitor.DisplayLabel);
        }

        if (IdentityEquals(gameMonitor, companionMonitor))
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
                    $"The duplicate monitor selection was blocked; Companion will use {companionMonitor.DisplayLabel}."));
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

        if (FindByReference(monitors, gameMonitor.IdentityKey) is null)
        {
            issues.Add(MonitorSelectionIssue.FromFormat(
                MonitorSelectionIssueCode.GameMonitorUnavailable,
                MonitorSelectionIssueSeverity.Error,
                $"The selected game monitor {gameMonitor.DisplayLabel} is not available."));
        }

        if (FindByReference(monitors, companionMonitor.IdentityKey) is null)
        {
            issues.Add(MonitorSelectionIssue.FromFormat(
                MonitorSelectionIssueCode.CompanionMonitorUnavailable,
                MonitorSelectionIssueSeverity.Error,
                $"The selected Companion monitor {companionMonitor.DisplayLabel} is not available."));
        }

        if (IdentityEquals(gameMonitor, companionMonitor))
        {
            var distinctMonitorCount = monitors
                .Select(static monitor => monitor.IdentityKey)
                .Distinct(IdentityComparer)
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

    public static MonitorProfile? FindByReference(IReadOnlyList<MonitorProfile> monitors, string? reference)
    {
        ArgumentNullException.ThrowIfNull(monitors);
        if (string.IsNullOrWhiteSpace(reference)) return null;
        return monitors.FirstOrDefault(monitor =>
            IdentityComparer.Equals(monitor.IdentityKey, reference) ||
            IdentityComparer.Equals(monitor.DeviceName, reference));
    }

    public static bool IdentityEquals(MonitorProfile left, MonitorProfile right) =>
        IdentityComparer.Equals(left.IdentityKey, right.IdentityKey);

    internal static bool DeviceNamesEqual(string left, string right) => IdentityComparer.Equals(left, right);

    private static MonitorProfile? FindConfiguredMonitor(
        IReadOnlyList<MonitorProfile> monitors,
        string? stableId,
        string? legacyDeviceName)
    {
        // Once a stable id exists, never fall back to a possibly reassigned DISPLAYn alias.
        if (!string.IsNullOrWhiteSpace(stableId))
            return monitors.FirstOrDefault(monitor => IdentityComparer.Equals(monitor.IdentityKey, stableId));
        return FindByReference(monitors, legacyDeviceName);
    }

    private static List<MonitorProfile> ValidateAndCopyMonitors(IReadOnlyList<MonitorProfile> availableMonitors)
    {
        var monitors = new List<MonitorProfile>(availableMonitors.Count);
        var deviceNames = new HashSet<string>(IdentityComparer);
        var stableIds = new HashSet<string>(IdentityComparer);

        foreach (var monitor in availableMonitors)
        {
            if (monitor is null)
                throw new ArgumentException("The monitor list cannot contain null entries.", nameof(availableMonitors));
            if (string.IsNullOrWhiteSpace(monitor.DeviceName))
                throw new ArgumentException("Every monitor must have a device name.", nameof(availableMonitors));
            if (!deviceNames.Add(monitor.DeviceName))
                throw new ArgumentException($"The monitor list contains the duplicate device name {monitor.DeviceName}.", nameof(availableMonitors));
            if (!string.IsNullOrWhiteSpace(monitor.StableId) && !stableIds.Add(monitor.StableId))
                throw new ArgumentException($"The monitor list contains the duplicate persistent id {monitor.StableId}.", nameof(availableMonitors));
            monitors.Add(monitor);
        }

        return monitors;
    }

    private static MonitorProfile? FindFirstDifferentMonitor(IReadOnlyList<MonitorProfile> monitors, MonitorProfile selectedMonitor) =>
        monitors.FirstOrDefault(monitor => !IdentityEquals(monitor, selectedMonitor));

    private static void AddUnavailableIssueIfConfigured(
        ICollection<MonitorSelectionIssue> issues,
        string? configuredReference,
        MonitorSelectionIssueCode code,
        string role,
        string fallbackLabel)
    {
        if (!string.IsNullOrWhiteSpace(configuredReference))
        {
            issues.Add(MonitorSelectionIssue.FromFormat(
                code,
                MonitorSelectionIssueSeverity.Warning,
                $"The configured {role} monitor {configuredReference} is unavailable; using {fallbackLabel}."));
        }
    }
}
