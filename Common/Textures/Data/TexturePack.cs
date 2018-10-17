using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ByteSizeLib;

namespace Common.Textures.Data
{
    public enum TextureCompression : uint
    {
        Unknown = 0,
        Dxt1 = 0x31545844,
        Dxt3 = 0x33545844,
        Dxt5 = 0x35545844,
        Ati1 = 0x31495441,
        Ati2 = 0x32495441,
        A8R8G8B8 = 0x15,
        P8 = 0x29
    }

    public class Texture : ChunkManager.BasicResource
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

        public uint NameLength { get; set; }

        public TextureCompression CompressionType { get; set; }

        public Dictionary<string, object> Properties = new Dictionary<string, object>();

        public string Format => $"{CompressionType}".ToUpper();

        public string Dimensions => $"{Width}x{Height}";

        private byte[] _cachedDds = new byte[0];

        /// <summary>
        /// Generates a DDS byte array.
        /// </summary>
        /// <returns></returns>
        public byte[] GenerateImage()
        {
            if (_cachedDds.Length > 0)
            {
                return _cachedDds;
            }

            var ms = new MemoryStream();
            var bw = new BinaryWriter(ms);

            var dh = new DDSHeader();
            dh.Init(this);

            bw.PutStruct(dh);
            bw.Write(Data);

            var outData = ms.ToArray();

            ms.Dispose();
            bw.Dispose();

            _cachedDds = outData;

            return outData;
        }

        public void DumpToFile(string path)
        {
            var data = GenerateImage();

            using (var fs = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
            {
                fs.Write(data, 0, data.Length);
            }
        }
    }

    public class TexturePack : ChunkManager.BasicResource
    {
        public string Name { get; set; }

        public string PipelinePath { get; set; }

        public uint Hash { get; set; }

        public uint Version { get; set; }

        public uint TpkSize { get; set; }

        public uint TpkOffset { get; set; }

        public string Offset => $"0x{TpkOffset:X8}";

        public string Size => ByteSize.FromBytes(TpkSize).ToString();

        public List<Texture> Textures { get; set; } = new List<Texture>();

        public int NumTextures => Textures.Count;

        public Texture Find(uint hash) => Textures.Find(t => t.TexHash == hash);
        public Texture Find(string name) => Textures.Find(t => t.Name.Contains(name));
    }
}
