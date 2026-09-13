using System.Runtime.ExceptionServices;
using GameTouchCompanion.Core;

namespace GameTouchCompanion.Native;

public sealed class Win32MonitorService : IMonitorService
{
    public IReadOnlyList<MonitorProfile> GetMonitors()
    {
        var monitors = new List<MonitorProfile>();
        ExceptionDispatchInfo? callbackFailure = null;

        NativeMethods.EnumerateDisplayMonitors((nint monitor, nint _, ref NativeRect _, nint _) =>
        {
            if (callbackFailure is not null)
                return true;

            try
            {
                var info = NativeMethods.ReadMonitorInfo(monitor);
                var snapshot = new MonitorSnapshot(
                    info.ReadDeviceName(),
                    info.MonitorBounds,
                    info.WorkingArea,
                    info.Flags);

                monitors.Add(MonitorProfileMapper.ToMonitorProfile(snapshot));
            }
            catch (Exception exception)
            {
                // Exceptions must never cross the unmanaged callback boundary.
                callbackFailure = ExceptionDispatchInfo.Capture(exception);
            }

            return true;
        });

        callbackFailure?.Throw();
        return MonitorProfileMapper.Order(monitors);
    }
}
