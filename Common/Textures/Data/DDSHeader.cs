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
            Flags = 0x1 | 0x2 | 0x4;

            if (texture.CompressionType == TextureCompression.P8 ||
                texture.CompressionType == TextureCompression.A8R8G8B8)
            {
                Flags |= 0x8;
                Flags |= 0x00001000;
                Flags |= 0x00020000;
            }
            else
            {
                Flags |= 0x00001000;
                Flags |= 0x00020000;
                Flags |= 0x00080000;
            }

            Width = (int)texture.Width;
            Height = (int)texture.Height;
            Depth = 1;
            MipMapCount = (int)texture.MipMapCount;

            PixelFormat.Size = 0x20;
            PixelFormat.RGBBitCount = 0x20;

            if (texture.CompressionType == TextureCompression.Dxt1
                || texture.CompressionType == TextureCompression.Dxt3
                || texture.CompressionType == TextureCompression.Dxt5
                || texture.CompressionType == TextureCompression.Ati1
                || texture.CompressionType == TextureCompression.Ati2)
            {
                PixelFormat.Flags = 0x4;
                PixelFormat.FourCC = (int)texture.CompressionType;
                PitchOrLinearSize = texture.Data.Length;
            }
            else
            {
                PixelFormat.Flags = 0x41;

                if (texture.CompressionType == TextureCompression.P8)
                {
                    PixelFormat.RBitMask = 0x000000ff;
                    PixelFormat.GBitMask = 0x0000ff00;
                    PixelFormat.BBitMask = 0x00ff0000;
                    PixelFormat.AlphaBitMask = unchecked((int)0xff000000);
                }
                else if (texture.CompressionType == TextureCompression.A8R8G8B8)
                {
                    PixelFormat.RBitMask = 0x00ff0000;
                    PixelFormat.GBitMask = 0x0000ff00;
                    PixelFormat.BBitMask = 0x000000ff;
                    PixelFormat.AlphaBitMask = unchecked((int)0xff000000);
                }
                else
                {
                    throw new Exception("What happened?");
                }

                PitchOrLinearSize = (int)Math.Floor((double)((Width * 0x20) / 8));
            }

            DDSCaps.Caps1 = 0x00001000;

            //texture.PitchOrLinearSize
        }
    }
}
