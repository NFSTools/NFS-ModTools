using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Common;
using TexEd.Data;

namespace TexEd.Games
{
    /// <summary>
    /// TPK support for NFS:MW.
    /// </summary>
    public class MostWantedTpk : TpkManager
    {
        private const uint InfoChunkId = 0x33310001;
        private const uint HashChunkId = 0x33310002;
        private const uint TexChunkId = 0x33310004;
        private const uint DDSChunkId = 0x33310005;
        private const uint TexDataChunkId = 0x33320002;

        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 124)]
        private struct TextureStruct
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
            private byte[] blank;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 24)]
            public string Name;

            public uint Hash;

            public uint Type;

            private int blank2;

            public uint DataOffset;

            public uint PaletteOffset;

            public uint DataSize, PaletteSize, PitchOrLinearSize;

            public ushort Width, Height;

            public uint D1;

            public ushort D2;

            public byte D3; // mipmap?
            public byte D4;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 44)]
            public byte[] RestOfData;
        }

        private TexturePack _texturePack;

        private uint _offset, _size;

        public override TexturePack ReadTexturePack(BinaryReader br, uint containerSize)
        {
            _texturePack = new TexturePack();
            _offset = (uint)(br.BaseStream.Position - 8);
            _size = containerSize + 8;

            ReadChunks(br, containerSize);

            return _texturePack;
        }

        protected override void ReadChunks(BinaryReader br, uint containerSize)
        {
            var endPos = br.BaseStream.Position + containerSize;

            while (br.BaseStream.Position < endPos)
            {
                var chunkId = br.ReadUInt32();
                var chunkSize = br.ReadUInt32();
                var chunkEndPos = br.BaseStream.Position + chunkSize;

                if ((chunkId & 0x80000000) == 0x80000000)
                {
                    ReadChunks(br, chunkSize);
                }
                else
                {
                    switch (chunkId)
                    {
                        case InfoChunkId:
                            {
                                _texturePack.Version = br.ReadUInt32();
                                _texturePack.Name = new string(br.ReadChars(28)).Trim('\0');
                                _texturePack.PipelinePath = new string(br.ReadChars(64)).Trim('\0');
                                _texturePack.Hash = br.ReadUInt32();
                                _texturePack.TpkOffset = _offset;
                                _texturePack.TpkSize = _size;

                                break;
                            }
                        case TexChunkId:
                            {
                                var textureStructSize = Marshal.SizeOf<TextureStruct>();
                                Debug.Assert(chunkSize % textureStructSize == 0);

                                var numTextures = chunkSize / textureStructSize;

                                for (var j = 0; j < numTextures; j++)
                                {
                                    var texture = BinaryUtil.ReadStruct<TextureStruct>(br);

                                    _texturePack.Textures.Add(new Texture
                                    {
                                        Width = texture.Width,
                                        Height = texture.Height,
                                        Name = texture.Name,
                                        Data = new byte[texture.DataSize],
                                        DataSize = texture.DataSize,
                                        DataOffset = texture.DataOffset,
                                        MipMapCount = texture.D3,
                                        TexHash = texture.Hash,
                                        TypeHash = texture.Type,
                                        CompressionType = TextureCompression.Unknown,
                                        PitchOrLinearSize = texture.PitchOrLinearSize
                                    });
                                }

                                break;
                            }
                        case TexDataChunkId:
                            {
                                br.BaseStream.Position += 0x78;

                                var basePos = br.BaseStream.Position;

                                foreach (var t in _texturePack.Textures)
                                {
                                    br.BaseStream.Position = basePos + t.DataOffset;
                                    br.Read(t.Data, 0, t.Data.Length);
                                }

                                break;
                            }
                        case DDSChunkId:
                            {
                                foreach (var t in _texturePack.Textures)
                                {
                                    br.BaseStream.Seek(20, SeekOrigin.Current);
                                    t.CompressionType = (TextureCompression) br.ReadUInt32();
                                    Console.WriteLine($"{t.Name} = 0x{((int)t.CompressionType):X8}");
                                    br.BaseStream.Seek(0x08, SeekOrigin.Current);
                                }
                                break;
                            }
                    }
                }

                br.BaseStream.Position = chunkEndPos;
            }
        }
    }
}
