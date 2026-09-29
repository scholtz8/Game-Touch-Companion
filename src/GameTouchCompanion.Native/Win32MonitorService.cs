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
                var deviceName = info.ReadDeviceName();
                DisplayIdentity identity;
                try
                {
                    identity = NativeMethods.ReadDisplayIdentity(deviceName);
                }
                catch
                {
                    // Persistent identity enrichment must never make basic monitor enumeration fail.
                    identity = default;
                }

                var snapshot = new MonitorSnapshot(
                    deviceName,
                    info.MonitorBounds,
                    info.WorkingArea,
                    info.Flags,
                    identity.StableId,
                    identity.FriendlyName);

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
