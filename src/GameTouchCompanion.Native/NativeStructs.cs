using System.Runtime.InteropServices;

namespace GameTouchCompanion.Native;

[StructLayout(LayoutKind.Sequential)]
internal struct NativeRect
{
    internal int Left;
    internal int Top;
    internal int Right;
    internal int Bottom;

    internal NativeRect(int left, int top, int right, int bottom)
    {
        Left = left;
        Top = top;
        Right = right;
        Bottom = bottom;
    }
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal unsafe struct MonitorInfoEx
{
    internal uint Size;
    internal NativeRect MonitorBounds;
    internal NativeRect WorkingArea;
    internal uint Flags;
    internal fixed char DeviceName[NativeConstants.CchDeviceName];

    internal static MonitorInfoEx Create() => new()
    {
        Size = (uint)sizeof(MonitorInfoEx),
    };

    internal string ReadDeviceName()
    {
        fixed (char* start = DeviceName)
        {
            var characters = new ReadOnlySpan<char>(start, NativeConstants.CchDeviceName);
            var terminator = characters.IndexOf('\0');
            return new string(terminator >= 0 ? characters[..terminator] : characters);
        }
    }
}
