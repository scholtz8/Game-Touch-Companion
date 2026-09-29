using System.ComponentModel;
using System.Runtime.InteropServices;

namespace GameTouchCompanion.Native;

internal static partial class NativeMethods
{
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal delegate bool MonitorEnumProc(
        nint monitor,
        nint deviceContext,
        ref NativeRect monitorBounds,
        nint data);

    [LibraryImport("user32.dll")]
    internal static partial nint GetForegroundWindow();

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool EnumDisplayMonitors(
        nint deviceContext,
        nint clipRectangle,
        MonitorEnumProc callback,
        nint data);

    [LibraryImport("user32.dll", SetLastError = true, EntryPoint = "GetMonitorInfoW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetMonitorInfo(nint monitor, ref MonitorInfoEx monitorInfo);

    [LibraryImport("user32.dll", EntryPoint = "EnumDisplayDevicesW", StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool EnumDisplayDevices(string? device, uint deviceNumber, ref DisplayDevice displayDevice, uint flags);

    [LibraryImport("user32.dll", SetLastError = true, EntryPoint = "GetWindowLongPtrW")]
    private static partial nint GetWindowLongPtr64(nint hWnd, int nIndex);

    [LibraryImport("user32.dll", SetLastError = true, EntryPoint = "SetWindowLongPtrW")]
    private static partial nint SetWindowLongPtr64(nint hWnd, int nIndex, nint newLong);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool ShowWindow(nint hWnd, int command);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetWindowPos(nint hWnd, nint insertAfter, int x, int y, int cx, int cy, uint flags);

    internal static void EnumerateDisplayMonitors(MonitorEnumProc callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        Marshal.SetLastPInvokeError(0);
        if (!EnumDisplayMonitors(nint.Zero, nint.Zero, callback, nint.Zero))
            ThrowWin32Failure(nameof(EnumDisplayMonitors));
    }

    internal static MonitorInfoEx ReadMonitorInfo(nint monitor)
    {
        var monitorInfo = MonitorInfoEx.Create();
        Marshal.SetLastPInvokeError(0);

        if (!GetMonitorInfo(monitor, ref monitorInfo))
            ThrowWin32Failure(nameof(GetMonitorInfo));

        return monitorInfo;
    }

    internal static DisplayIdentity ReadDisplayIdentity(string gdiDeviceName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gdiDeviceName);

        for (uint adapterIndex = 0; ; adapterIndex++)
        {
            var adapter = DisplayDevice.Create();
            if (!EnumDisplayDevices(null, adapterIndex, ref adapter, 0)) break;
            if (!string.Equals(adapter.ReadDeviceName(), gdiDeviceName, StringComparison.OrdinalIgnoreCase)) continue;

            // The monitor interface path is independent from the transient \.\DISPLAYn GDI alias.
            // It is the preferred persisted identity. If Windows cannot expose it, fall back to
            // the monitor device id/key; callers can still fall back to the GDI alias as a last resort.
            for (uint monitorIndex = 0; monitorIndex < 16; monitorIndex++)
            {
                var monitor = DisplayDevice.Create();
                if (!EnumDisplayDevices(gdiDeviceName, monitorIndex, ref monitor, NativeConstants.EddGetDeviceInterfaceName)) break;

                var stableId = FirstNonEmpty(monitor.ReadDeviceId(), monitor.ReadDeviceKey());
                var friendlyName = FirstNonEmpty(monitor.ReadDeviceString(), adapter.ReadDeviceString());
                if (!string.IsNullOrWhiteSpace(stableId) || !string.IsNullOrWhiteSpace(friendlyName))
                    return new DisplayIdentity(NormalizeIdentity(stableId), friendlyName?.Trim());
            }

            return new DisplayIdentity(null, FirstNonEmpty(adapter.ReadDeviceString())?.Trim());
        }

        return default;
    }

    private static string? FirstNonEmpty(params string[] values) =>
        values.FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value));

    private static string? NormalizeIdentity(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

    internal static nint GetExtendedStyle(nint hwnd)
    {
        Marshal.SetLastPInvokeError(0);
        var result = GetWindowLongPtr64(hwnd, NativeConstants.GwlExStyle);
        ThrowIfZeroWithError(result);
        return result;
    }

    internal static void SetExtendedStyle(nint hwnd, nint style)
    {
        Marshal.SetLastPInvokeError(0);
        var result = SetWindowLongPtr64(hwnd, NativeConstants.GwlExStyle, style);
        ThrowIfZeroWithError(result);
    }

    internal static void KeepPositionWithoutActivation(nint hwnd)
    {
        var flags = NativeConstants.SwpNoActivate | NativeConstants.SwpNoMove |
                    NativeConstants.SwpNoSize | NativeConstants.SwpNoZOrder;

        Marshal.SetLastPInvokeError(0);
        if (!SetWindowPos(hwnd, nint.Zero, 0, 0, 0, 0, flags))
            ThrowWin32Failure(nameof(SetWindowPos));
    }

    internal static void SetWindowBoundsWithoutActivation(nint hwnd, WindowPlacement placement)
    {
        Marshal.SetLastPInvokeError(0);
        if (!SetWindowPos(
                hwnd,
                nint.Zero,
                placement.X,
                placement.Y,
                placement.Width,
                placement.Height,
                placement.Flags))
        {
            ThrowWin32Failure(nameof(SetWindowPos));
        }
    }

    private static void ThrowIfZeroWithError(nint result)
    {
        var error = Marshal.GetLastPInvokeError();
        if (result == nint.Zero && error != 0)
            throw new Win32Exception(error);
    }

    private static void ThrowWin32Failure(string operation)
    {
        var error = Marshal.GetLastPInvokeError();
        if (error != 0)
            throw new Win32Exception(error);

        throw new Win32Exception($"{operation} failed without a Win32 error code.");
    }
}
