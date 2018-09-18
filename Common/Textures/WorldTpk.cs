using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Common.Textures.Data;

namespace Common.Textures
{
    /// <summary>
    /// TPK support for NFS:W.
    /// </summary>
    public class WorldTpk : TpkManager
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

            public uint TexHash, TypeHash;

            public int Unknown1;

            public uint DataSize;

            public int Unknown2;

            public uint Width, Height, MipMapCount;

            public int Unknown3, Unknown4;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)]
            public byte[] Unknown5;

            public int Unknown6;

            public uint DataOffset;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 60)]
            public byte[] Unknown7;

            public byte NameLength;
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
                        case HashChunkId:
                        {
                            _texturePack.Textures = new List<Texture>((int) (chunkSize / 8));
                            break;
                        }
                        case TexChunkId:
                            {
                                for (var j = 0; j < _texturePack.Textures.Capacity; j++)
                                {
                                    var texture = BinaryUtil.ReadStruct<TextureStruct>(br);
                                    var name = new string(br.ReadChars(texture.NameLength)).TrimEnd('\0');

                                    _texturePack.Textures.Add(new Texture
                                    {
                                        Width = texture.Width,
                                        Height = texture.Height,
                                        Name = name,
                                        Data = new byte[texture.DataSize],
                                        DataSize = texture.DataSize,
                                        DataOffset = texture.DataOffset,
                                        MipMapCount = texture.MipMapCount,
                                        TexHash = texture.TexHash,
                                        TypeHash = texture.TypeHash,
                                        CompressionType = TextureCompression.Unknown,
                                        PitchOrLinearSize = 0
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
                                    // BC4 = 42 43 34 
                                    // BC5 = 42 43 35

                                    br.BaseStream.Seek(12, SeekOrigin.Current);

                                    var compType = br.ReadUInt32();

                                    //if (compType == 0x31495441)
                                    //{
                                    //    compType = 0x0034
                                    //}

                                    t.CompressionType = (TextureCompression) compType;
                              
                                    //t.CompressionType = (TextureCompression)br.ReadUInt32();
                                    //Console.WriteLine($"{t.Name} = 0x{((int)t.CompressionType):X8}");
                                    br.BaseStream.Seek(16, SeekOrigin.Current);
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
