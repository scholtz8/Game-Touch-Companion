namespace GameTouchCompanion.Core;

/// <summary>
/// Describes a monitor discovered by the platform-specific monitor service.
/// </summary>
public sealed record MonitorProfile(
    string DeviceName,
    DisplayRect Bounds,
    DisplayRect WorkingArea,
    bool IsPrimary)
{
    public string DisplayLabel =>
        $"{DeviceName} — {Bounds.Width} × {Bounds.Height} @ ({Bounds.X}, {Bounds.Y})" +
        (IsPrimary ? " — Primary" : string.Empty);

    public override string ToString() => DisplayLabel;
}
