using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ByteSizeLib;

namespace TexEd.Data
{
    public enum TextureCompression : uint
    {
        Unknown = 0,
        Dxt1 = 0x31545844,
        Dxt3 = 0x33545844,
        Dxt5 = 0x35545844,
        Ati1 = 0x31495441,
        Ati2 = 0x32495441,
        A8R8G8B8 = 21,
        P8 = 41
    }

    public class Texture
    {
        public string Name { get; set; }

        public uint TexHash { get; set; }

        public uint TypeHash { get; set; }

        public uint Width { get; set; }

        public uint Height { get; set; }

        public uint MipMapCount { get; set; }
        
        public uint DataSize { get; set; }

        public uint DataOffset { get; set; }

        public byte[] Data { get; set; }

        public uint PitchOrLinearSize { get; set; }

        public TextureCompression CompressionType { get; set; }

        public Dictionary<string, object> Properties = new Dictionary<string, object>();

        public string Format => $"{CompressionType}".ToUpper();

        public string Dimensions => $"{Width}x{Height}";
    }

    public class TexturePack
    {
        public string Name { get; set; }

        public string PipelinePath { get; set; }

        public uint Hash { get; set; }

        public uint Version { get; set; }

        public uint TpkSize { get; set; }

        public uint TpkOffset { get; set; }

        public string Offset => $"0x{TpkOffset:X8}";

        public string Size => ByteSize.FromBytes(TpkSize).ToString();

        public List<Texture> Textures { get; } = new List<Texture>();

        public int NumTextures => Textures.Count;
    }
}
