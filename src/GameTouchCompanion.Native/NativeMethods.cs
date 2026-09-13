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
