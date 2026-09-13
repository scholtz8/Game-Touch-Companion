namespace GameTouchCompanion.Native;

public static class NativeConstants
{
    public const int GwlExStyle = -20;
    public const long WsExNoActivate = 0x08000000L;
    public const long WsExToolWindow = 0x00000080L;
    public const int WmMouseActivate = 0x0021;
    public const int MaNoActivate = 3;
    public const int WmActivate = 0x0006;
    public const int WmSetFocus = 0x0007;
    public const int WmKillFocus = 0x0008;
    public const int WmActivateApp = 0x001C;
    public const int WmDisplayChange = 0x007E;
    public const int WmDeviceChange = 0x0219;
    public const int WmDpiChanged = 0x02E0;
    public const int SwShowNoActivate = 4;
    public const uint SwpNoActivate = 0x0010;
    public const uint SwpNoMove = 0x0002;
    public const uint SwpNoSize = 0x0001;
    public const uint SwpNoZOrder = 0x0004;
    public const uint MonitorInfoPrimary = 0x00000001;
    public const int CchDeviceName = 32;
}
