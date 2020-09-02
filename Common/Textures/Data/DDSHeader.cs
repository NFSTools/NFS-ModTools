using System;
using System.Runtime.InteropServices;

namespace Common.Textures.Data
{
    [StructLayout(LayoutKind.Sequential)]
    public struct PixelFormat
    {
        public int Size;
        public int Flags;
        public int FourCC;
        public int RGBBitCount;
        public int RBitMask;
        public int GBitMask;
        public int BBitMask;
        public int AlphaBitMask;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DDSCaps
    {
        public int Caps1;
        public int Caps2;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public int[] Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DDSHeader
    {
        public int Magic;
        public int Size;
        public int Flags;
        public int Height;
        public int Width;
        public int PitchOrLinearSize;
        public int Depth;
        public int MipMapCount;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 11)]
        public int[] Reserved1;

        public PixelFormat PixelFormat;
        public DDSCaps DDSCaps;

        public int Reserved2;

        /// <summary>
        /// Initialize the DDS header structure with the given texture data.
        /// </summary>
        /// <param name="texture"></param>
        public void Init(Texture texture)
        {
            Magic = 0x20534444; // "DDS "
            Size = 0x7C;
            Width = (int) texture.Width;
            Height = (int) texture.Height;
            MipMapCount = 0;
            PixelFormat.Size = 32;
            PitchOrLinearSize = texture.Data.Length;

            if ((int) texture.CompressionType == 0x15 || (int)texture.CompressionType == 0x29)
            {
                PixelFormat.Flags = 0x41;
                PixelFormat.RGBBitCount = 0x20;
                PixelFormat.RBitMask = 0xFF0000;
                PixelFormat.GBitMask = 0xFF00;
                PixelFormat.BBitMask = 0xFF;
                PixelFormat.AlphaBitMask = unchecked((int) 0xFF000000);
                DDSCaps.Caps1 = 0x40100a;
            }
            else
            {
                PixelFormat.Flags = 0x4;
                PixelFormat.FourCC = (int) texture.CompressionType;
                DDSCaps.Caps1 = 0x401008;
            }
        }
    }
}
