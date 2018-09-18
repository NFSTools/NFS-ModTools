using System.Runtime.InteropServices;

namespace Common
{
    public static class Compression
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct CompressBlockHead
        {
            public uint CompressBlockMagic; // = 0x55441122
            public uint OutSize; // =0x8000
            public uint TotalBlockSize; // Skip back to before CompressBlockMagic, then jump TotalBlockSize to get to the next block (or, subtract 24)
            public uint Unknown2; // += OutSize
            public uint Unknown3; // += TotalBlockSize
            public uint Unknown4;
        }

        [DllImport("complib", EntryPoint = "LZDecompress", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Decompress(
            [In] byte[] inData,
            [Out] byte[] outData
        );
    }
}
