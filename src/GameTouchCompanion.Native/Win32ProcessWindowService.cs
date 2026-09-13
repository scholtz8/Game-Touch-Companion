using System.ComponentModel;
using System.Runtime.InteropServices;
using GameTouchCompanion.Core;

namespace GameTouchCompanion.Native;

public sealed partial class Win32ProcessWindowService : IProcessWindowService
{
    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private delegate bool WindowCallback(nint window, nint data);
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool EnumWindows(WindowCallback callback, nint data);
    [LibraryImport("user32.dll")]
    private static partial uint GetWindowThreadProcessId(nint window, out uint processId);
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool IsWindowVisible(nint window);
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool IsIconic(nint window);
    [LibraryImport("user32.dll")]
    private static partial nint GetWindow(nint window, uint command);
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetWindowRect(nint window, out NativeRect rect);

    public nint GetForegroundWindow() => NativeMethods.GetForegroundWindow();
    public IReadOnlyList<ProcessWindowInfo> GetWindows()
    {
        var result = new List<ProcessWindowInfo>();
        // Window destruction between calls is normal. Never throw across the native callback boundary.
        bool Visit(nint window, nint data)
        {
            try
            {
                if (GetWindowThreadProcessId(window, out var pid) == 0 || pid > int.MaxValue ||
                    !GetWindowRect(window, out var rect)) return true;
                var width = (long)rect.Right - rect.Left;
                var height = (long)rect.Bottom - rect.Top;
                if (width < 0 || width > int.MaxValue || height < 0 || height > int.MaxValue) return true;
                result.Add(new ProcessWindowInfo(window, (int)pid,
                    new DisplayRect(rect.Left, rect.Top, (int)width, (int)height), IsWindowVisible(window),
                    IsIconic(window), GetWindow(window, 4) != 0,
                    (NativeMethods.GetExtendedStyle(window).ToInt64() & NativeConstants.WsExToolWindow) != 0));
            }
            catch (Win32Exception) { /* A stale/inaccessible HWND is not a valid candidate. */ }
            return true;
        }
        Marshal.SetLastPInvokeError(0);
        if (!EnumWindows(Visit, 0))
        {
            var error = Marshal.GetLastPInvokeError();
            throw error == 0 ? new Win32Exception("EnumWindows failed without an error code; desktop enumeration is unavailable.") : new Win32Exception(error);
        }
        return result;
    }
}
