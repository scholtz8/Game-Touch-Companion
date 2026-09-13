namespace GameTouchCompanion.Native;

public static class NoActivateWindowService
{
    public static nint ComposeExtendedStyle(nint currentStyle) =>
        currentStyle | (nint)(NativeConstants.WsExNoActivate | NativeConstants.WsExToolWindow);

    public static bool TryHandleMessage(int message, out nint result)
    {
        result = message == NativeConstants.WmMouseActivate ? NativeConstants.MaNoActivate : nint.Zero;
        return message == NativeConstants.WmMouseActivate;
    }

    public static void Apply(nint hwnd)
    {
        var style = NativeMethods.GetExtendedStyle(hwnd);
        NativeMethods.SetExtendedStyle(hwnd, ComposeExtendedStyle(style));
        NativeMethods.ShowWindow(hwnd, NativeConstants.SwShowNoActivate);
        NativeMethods.KeepPositionWithoutActivation(hwnd);
    }
}
