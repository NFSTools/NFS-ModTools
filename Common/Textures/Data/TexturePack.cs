using System;
using System.Collections.Generic;
using System.IO;

namespace Common.Textures.Data
{
    public enum TextureCompressionType : byte
    {
        TEXCOMP_DEFAULT = 0x0,
        TEXCOMP_4BIT = 0x4,
        TEXCOMP_8BIT = 0x8,
        TEXCOMP_16BIT = 0x10,
        TEXCOMP_24BIT = 0x18,
        TEXCOMP_32BIT = 0x20,
        TEXCOMP_DXT = 0x21,
        TEXCOMP_S3TC = 0x22,
        TEXCOMP_DXTC1 = 0x22,
        TEXCOMP_DXTC3 = 0x24,
        TEXCOMP_DXTC5 = 0x26,
        TEXCOMP_DXTN = 0x27,
        TEXCOMP_L8 = 0x28,
        TEXCOMP_DXTC1_AIR = 0x29,
        TEXCOMP_DXTC1_AIG = 0x2A,
        TEXCOMP_DXTC1_AIB = 0x2B,
        TEXCOMP_16BIT_1555 = 0x11,
        TEXCOMP_16BIT_565 = 0x12,
        TEXCOMP_16BIT_3555 = 0x13,
        TEXCOMP_8BIT_16 = 0x80,
        TEXCOMP_8BIT_64 = 0x81,
        TEXCOMP_8BIT_IA8 = 0x82,
        TEXCOMP_4BIT_IA8 = 0x83,
        TEXCOMP_4BIT_RGB24_A8 = 0x8C,
        TEXCOMP_8BIT_RGB24_A8 = 0x8D,
        TEXCOMP_4BIT_RGB16_A8 = 0x8E,
        TEXCOMP_8BIT_RGB16_A8 = 0x8F,
    }

    public class Texture : BasicResource
    {
        public string Name { get; set; }

        public uint TexHash { get; set; }

        public uint TypeHash { get; set; }

        public uint Width { get; set; }

        public uint Height { get; set; }

        public uint MipMapCount { get; set; }

        public uint DataSize { get; set; }

        public uint DataOffset { get; set; }

        public uint PaletteSize { get; set; }

        public uint PaletteOffset { get; set; }

        public byte[] Palette { get; set; }
        public byte[] Data { get; set; }

        public uint PitchOrLinearSize { get; set; }

        public uint Format { get; set; }
        public TextureCompressionType CompressionType { get; set; }

        /// <summary>
        /// Writes DDS data to the given stream.
        /// </summary>
        /// <returns></returns>
        public void GenerateImage(Stream stream)
        {
            var bw = new BinaryWriter(stream);

            var dh = new DDSHeader();
            dh.Init(this);

            bw.PutStruct(dh);

            if (Format == 0x29)
            {
                var palette = new uint[0x100];
                var result = new byte[Data.Length * 4];

                for (var loop = 0; loop < 0x100; ++loop)
                {
                    palette[loop] = BitConverter.ToUInt32(Palette, loop * 4);
                }

                for (var loop = 0; loop < Data.Length; ++loop)
                {
                    var color = palette[Data[loop]];
                    Array.ConstrainedCopy(BitConverter.GetBytes(color), 0, result, loop * 4, 4);
                }

                bw.Write(result);
            }
            else
            {
                bw.Write(Data);
            }
        }

        public void DumpToFile(string path)
        {
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
                GenerateImage(fs);
        }
    }

    public class TexturePack : BasicResource
    {
        public string Name { get; set; }

        public string PipelinePath { get; set; }

        public uint Hash { get; set; }

        public uint Version { get; set; }

        public List<Texture> Textures { get; set; } = new List<Texture>();

        public Texture Find(uint hash) => Textures.Find(t => t.TexHash == hash);
        public Texture Find(string name) => Textures.Find(t => t.Name.Contains(name));
    }
}
