using System;
using System.Runtime.InteropServices;

namespace Common.Textures.Data
{
    [StructLayout(LayoutKind.Sequential)]
    public struct PixelFormat
    {
        public uint Size;
        public uint Flags;
        public uint FourCC;
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

    static class DdsConstants
    {
        #region DDSStruct Flags
        public const int DdsdCaps = 0x00000001;
        public const int DdsdHeight = 0x00000002;
        public const int DdsdWidth = 0x00000004;
        public const int DdsdPitch = 0x00000008;
        public const int DdsdPixelformat = 0x00001000;
        public const int DdsdMipmapcount = 0x00020000;
        public const int DdsdLinearsize = 0x00080000;
        public const int DdsdDepth = 0x00800000;
        #endregion

        #region pixelformat values
        public const int DdpfAlphapixels = 0x00000001;
        public const int DdpfFourcc = 0x00000004;
        public const int DdpfRgb = 0x00000040;
        public const int DdpfLuminance = 0x00020000;
        #endregion

        #region ddscaps
        // caps1
        public const int DdscapsComplex = 0x00000008;
        public const int DdscapsTexture = 0x00001000;
        public const int DdscapsMipmap = 0x00400000;
        // caps2
        public const int Ddscaps2Cubemap = 0x00000200;
        public const int Ddscaps2CubemapPositivex = 0x00000400;
        public const int Ddscaps2CubemapNegativex = 0x00000800;
        public const int Ddscaps2CubemapPositivey = 0x00001000;
        public const int Ddscaps2CubemapNegativey = 0x00002000;
        public const int Ddscaps2CubemapPositivez = 0x00004000;
        public const int Ddscaps2CubemapNegativez = 0x00008000;
        public const int Ddscaps2Volume = 0x00200000;
        #endregion
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DDSHeader
    {
        public int Magic;
        public int Size;
        public uint Flags;
        public uint Height;
        public uint Width;
        public uint PitchOrLinearSize;
        public int Depth;
        public uint MipMapCount;

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
            Flags = DdsConstants.DdsdCaps | DdsConstants.DdsdHeight | DdsConstants.DdsdWidth |
                    DdsConstants.DdsdPixelformat | DdsConstants.DdsdMipmapcount;
            Height = texture.Height;
            Width = texture.Width;
            Depth = 1;
            MipMapCount = texture.MipMapCount;
            PixelFormat = new PixelFormat();
            DDSCaps = new DDSCaps();

            PixelFormat.Size = 0x20;
            DDSCaps.Caps1 = DdsConstants.DdscapsComplex | DdsConstants.DdscapsTexture |
                            DdsConstants.DdscapsMipmap;

            if ((texture.Format & 0x00545844) == 0x00545844) // DXT check
            {
                PitchOrLinearSize = /*Width * Height*/texture.PitchOrLinearSize;
                PixelFormat.Flags = DdsConstants.DdpfFourcc;
                PixelFormat.FourCC = texture.Format;
                Flags |= DdsConstants.DdsdLinearsize;
            }
            else if ((texture.Format & 0x00495441) == 0x00495441) // ATI check
            {
                PitchOrLinearSize = /*Width * Height*/texture.PitchOrLinearSize;
                PixelFormat.Flags = DdsConstants.DdpfFourcc;
                PixelFormat.FourCC = texture.Format;
                Flags |= DdsConstants.DdsdLinearsize;
            }
            else
            {
                PixelFormat.Flags = DdsConstants.DdpfAlphapixels | DdsConstants.DdpfRgb;
                PixelFormat.RGBBitCount = 0x20;
                PixelFormat.RBitMask = 16711680;
                PixelFormat.GBitMask = 65280;
                PixelFormat.BBitMask = 255;
                PixelFormat.AlphaBitMask = unchecked((int)4278190080);
                Flags |= DdsConstants.DdsdPitch;
                PitchOrLinearSize = /*(Width * 0x20 + 7) / 8*/texture.PitchOrLinearSize;
            }

            Reserved1 = new int[11];
        }
    }
}
