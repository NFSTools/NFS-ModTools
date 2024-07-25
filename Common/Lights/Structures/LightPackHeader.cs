using System.Runtime.InteropServices;

namespace Common.Lights.Structures;

[StructLayout(LayoutKind.Explicit, Size = 0x20)]
public struct LightPackHeader
{
    [FieldOffset(0x08)] public ushort Version;

    [FieldOffset(0x0C)] public uint ScenerySectionNumber;

    [FieldOffset(0x1C)] public int NumLights;
}