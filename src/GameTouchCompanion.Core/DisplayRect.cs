namespace GameTouchCompanion.Core;

/// <summary>
/// Describes a rectangle in signed physical-pixel virtual-desktop coordinates without depending on a UI framework.
/// </summary>
public readonly record struct DisplayRect(int X, int Y, int Width, int Height);
