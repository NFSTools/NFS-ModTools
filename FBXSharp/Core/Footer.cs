using System.Runtime.InteropServices;

namespace FBXSharp.Core
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Footer
    {
        public int Reserved;
        public uint Version;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x78)]
        public byte[] Padding;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
        public byte[] Magic;

        public const int SizeOf = 0x80;

        public static Footer CreateNew(uint version)
        {
            return new Footer
            {
                Reserved = 0,
                Version = version,
                Padding = new byte[0x78],
                Magic = new byte[0x10]
                {
                    0xF8,
                    0x5A,
                    0x8C,
                    0x6A,
                    0xDE,
                    0xF5,
                    0xD9,
                    0x7E,
                    0xEC,
                    0xE9,
                    0x0C,
                    0xE3,
                    0x75,
                    0x8F,
                    0x29,
                    0x0B
                }
            };
        }
    }
}