using System.Runtime.InteropServices;

namespace GameTouchCompanion.Native;

public static partial class WindowsShellAvailability
{
    public static bool HasNotificationArea => FindWindow("Shell_TrayWnd", null) != 0;

    [LibraryImport("user32.dll", EntryPoint = "FindWindowW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial nint FindWindow(string className, string? windowName);
}
