using System.Numerics;
using System.Runtime.InteropServices;

namespace Common.Lights.Structures;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct LightData
{
    public uint NameHash;
    public byte Type, AttenuationType, Shape, State;
    public uint ExcludeNameHash, Color;
    public Vector3 Position;
    public float Size;
    public Vector3 Direction;
    public float Intensity;
    public float FarStart;
    public float FarEnd;
    public float Falloff;
    public short ScenerySectionNumber;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 34)]
    public string Name;
}