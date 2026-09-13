namespace GameTouchCompanion.Native;

public interface IForegroundWindowService
{
    nint GetForegroundWindow();
}

public sealed class Win32ForegroundWindowService : IForegroundWindowService
{
    public nint GetForegroundWindow() => NativeMethods.GetForegroundWindow();
}
