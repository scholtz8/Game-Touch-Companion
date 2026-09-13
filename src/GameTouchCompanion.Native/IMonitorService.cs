using GameTouchCompanion.Core;

namespace GameTouchCompanion.Native;

public interface IMonitorService
{
    /// <summary>
    /// Enumerates the current desktop monitors. Rectangle values are physical-pixel
    /// coordinates in the Windows virtual desktop and may be negative.
    /// </summary>
    IReadOnlyList<MonitorProfile> GetMonitors();
}
