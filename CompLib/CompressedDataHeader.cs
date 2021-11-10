using System.Runtime.InteropServices;

namespace CompLib
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CompressedDataHeader
    {
        public uint ID;
        public byte Version;
        public byte HeaderSize;
        public ushort Flags;
        public uint UncompressedSize;
        public uint CompressedSize;
    }
}