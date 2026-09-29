using System.Security.Cryptography;
using System.Text;

namespace GameTouchCompanion.Core;

/// <summary>
/// Describes a monitor discovered by the platform-specific monitor service.
/// DeviceName is the current GDI name (for example \\.\DISPLAY2) and may change
/// between Windows sessions. StableId identifies the same monitor target across
/// DISPLAY-number changes whenever Windows exposes a persistent device path.
/// </summary>
public sealed record MonitorProfile(
    string DeviceName,
    DisplayRect Bounds,
    DisplayRect WorkingArea,
    bool IsPrimary,
    string? StableId = null,
    string? FriendlyName = null)
{
    public string IdentityKey => string.IsNullOrWhiteSpace(StableId) ? DeviceName : StableId;

    public string? StableTag => string.IsNullOrWhiteSpace(StableId)
        ? null
        : Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(StableId)))[..6];

    public string DisplayLabel
    {
        get
        {
            var name = string.IsNullOrWhiteSpace(FriendlyName) ? DeviceName : FriendlyName.Trim();
            var identity = StableTag is null ? string.Empty : $" · ID {StableTag}";
            return $"{name} — {Bounds.Width} × {Bounds.Height}{identity} @ ({Bounds.X}, {Bounds.Y})" +
                   (IsPrimary ? " — Primary" : string.Empty);
        }
    }

    public override string ToString() => DisplayLabel;
}
