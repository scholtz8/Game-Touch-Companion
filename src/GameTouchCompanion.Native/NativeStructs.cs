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

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal unsafe struct DisplayDevice
{
    internal uint Size;
    internal fixed char DeviceName[32];
    internal fixed char DeviceString[128];
    internal uint StateFlags;
    internal fixed char DeviceId[128];
    internal fixed char DeviceKey[128];

    internal static DisplayDevice Create() => new()
    {
        Size = (uint)sizeof(DisplayDevice),
    };

    internal string ReadDeviceName()
    {
        fixed (char* start = DeviceName) return Read(start, 32);
    }

    internal string ReadDeviceString()
    {
        fixed (char* start = DeviceString) return Read(start, 128);
    }

    internal string ReadDeviceId()
    {
        fixed (char* start = DeviceId) return Read(start, 128);
    }

    internal string ReadDeviceKey()
    {
        fixed (char* start = DeviceKey) return Read(start, 128);
    }

    private static string Read(char* start, int length)
    {
        var characters = new ReadOnlySpan<char>(start, length);
        var terminator = characters.IndexOf('\0');
        return new string(terminator >= 0 ? characters[..terminator] : characters);
    }
}

internal readonly record struct DisplayIdentity(string? StableId, string? FriendlyName);
