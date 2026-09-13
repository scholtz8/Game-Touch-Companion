using GameTouchCompanion.Core;

namespace GameTouchCompanion.Native;

public static class Win32WindowPlacementService
{
    /// <summary>
    /// Places a native window over the monitor's physical-pixel bounds without
    /// activating it or changing its z-order.
    /// </summary>
    public static void PlaceOnMonitor(nint windowHandle, MonitorProfile monitor)
    {
        ArgumentNullException.ThrowIfNull(monitor);

        if (windowHandle == nint.Zero)
            throw new ArgumentException("A valid window handle is required.", nameof(windowHandle));

        var placement = CreatePlacement(monitor.Bounds);
        NativeMethods.SetWindowBoundsWithoutActivation(windowHandle, placement);
    }

    internal static WindowPlacement CreatePlacement(DisplayRect bounds)
    {
        if (bounds.Width <= 0)
            throw new ArgumentOutOfRangeException(nameof(bounds), "Monitor width must be positive.");
        if (bounds.Height <= 0)
            throw new ArgumentOutOfRangeException(nameof(bounds), "Monitor height must be positive.");

        return new WindowPlacement(
            bounds.X,
            bounds.Y,
            bounds.Width,
            bounds.Height,
            NativeConstants.SwpNoActivate | NativeConstants.SwpNoZOrder);
    }
}

internal readonly record struct WindowPlacement(
    int X,
    int Y,
    int Width,
    int Height,
    uint Flags);
