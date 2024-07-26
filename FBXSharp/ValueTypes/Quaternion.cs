using System.Runtime.InteropServices;

namespace FBXSharp.ValueTypes
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Quaternion
    {
        public const int SizeOf = 0x20;

        public double X;
        public double Y;
        public double Z;
        public double W;
    }
}