using System.Runtime.InteropServices;

namespace VltEd.Structs
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VpakFileHeader
    {
        public readonly int FileNumber;
        public readonly int BinLength;
        public readonly int VaultLength;
        public readonly int BinLocation;
        public readonly int VaultLocation;
    }
}
