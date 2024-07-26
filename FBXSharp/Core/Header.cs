using System.Runtime.InteropServices;

namespace FBXSharp.Core
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Header
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x15)]
        public byte[] Magic;

        public byte Reserved1;
        public byte Reserved2;
        public uint Version;

        public const int SizeOf = 0x1B; // amazing lmao

        public static Header CreateNew(uint version)
        {
            return new Header
            {
                Version = version,
                Reserved1 = 0x1A,
                Reserved2 = 0x00,
                Magic = new byte[0x15]
                {
                    (byte)'K',
                    (byte)'a',
                    (byte)'y',
                    (byte)'d',
                    (byte)'a',
                    (byte)'r',
                    (byte)'a',
                    (byte)' ',
                    (byte)'F',
                    (byte)'B',
                    (byte)'X',
                    (byte)' ',
                    (byte)'B',
                    (byte)'i',
                    (byte)'n',
                    (byte)'a',
                    (byte)'r',
                    (byte)'y',
                    (byte)' ',
                    (byte)' ',
                    0
                }
            };
        }
    }
}