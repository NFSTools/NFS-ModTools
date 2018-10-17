using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
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

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct DataOffsetStruct
        {
            public uint Hash;
            public uint Offset;
            public uint LengthCompressed;
            public uint Length;
            public uint Flags;
            private uint blank;
            private uint blank2;
            private uint blank3;
            private uint blank4;
        }

        private TexturePack _texturePack;

        private uint _offset, _size;
        private bool _compressed = false;

        public override TexturePack ReadTexturePack(BinaryReader br, uint containerSize)
        {
            _texturePack = new TexturePack();
            _offset = (uint)(br.BaseStream.Position - 8);
            _size = containerSize + 8;

            ReadChunks(br, containerSize);

            return _texturePack;
        }

        public override void WriteTexturePack(ChunkStream cs, TexturePack texturePack)
        {
            cs.BeginChunk(0xb3300000);

            // pre null
            cs.BeginChunk(0x00000000);
            cs.Write(new byte[0x30]);
            cs.EndChunk();

            // tpk capsule
            cs.BeginChunk(0xb3310000);

            {
                // tpk info
                cs.BeginChunk(0x33310001);
                cs.Write(0x9);
                cs.Write(new FixedLenString(texturePack.Name, 28));
                cs.Write(new FixedLenString(texturePack.PipelinePath, 64));
                cs.Write(texturePack.Hash);
                cs.Write(new byte[0x18]);

                cs.EndChunk();

                // hash table
                cs.BeginChunk(0x33310002);
                foreach (var u in texturePack.Textures.Select(t => t.TexHash))
                {
                    cs.Write(u);
                    cs.Write(0x00000000);
                }
                cs.EndChunk();

                // textures
                cs.BeginChunk(0x33310004);

                {
                    var dataOffset = 0u;
                    var infoBytes = 0u;

                    foreach (var texture in texturePack.Textures)
                    {
                        var texStruct = new TextureStruct
                        {
                            TexHash = texture.TexHash,
                            TypeHash = texture.TypeHash,
                            DataOffset = dataOffset,
                            Height = texture.Height,
                            Width = texture.Width,
                            MipMapCount = texture.MipMapCount,
                            DataSize = (uint)texture.Data.Length
                        };

                        var padding = 128 - texture.Data.Length % 128;

                        if (padding < 48)
                        {
                            padding = 48;
                        }

                        dataOffset += (uint)texture.Data.Length;
                        dataOffset += (uint)padding;
                        infoBytes += 144;
                        infoBytes += (uint)(1 + texture.Name.Length);

                        var namePad = 4 - infoBytes % 4;

                        texStruct.NameLength = (byte)(texture.Name.Length + namePad);
                        cs.WriteStruct(texStruct);
                        cs.Write(Encoding.ASCII.GetBytes(texture.Name));
                        cs.Write(new byte[namePad]);
                    }
                }

                cs.EndChunk();

                // DDS compression info
                cs.BeginChunk(0x33310005);

                foreach (var texture in texturePack.Textures)
                {
                    cs.Write(new byte[0xC]);
                    cs.Write((uint)texture.CompressionType);
                    cs.Write(new byte[0x10]);
                }

                cs.EndChunk();
            }

            cs.EndChunk();

            // Data capsule
            {
                cs.PaddingAlignment(0x80);

                cs.BeginChunk(0xb3320000);

                cs.BeginChunk(0x33330001);
                cs.Write(new byte[4]);
                cs.EndChunk();

                cs.BeginChunk(0x33320001);
                cs.Write(0x0);
                cs.Write(0x0);
                cs.Write(0x2);
                cs.Write(texturePack.Hash);
                cs.Write(0x0);
                cs.Write(0x0);
                cs.Write(new byte[56]);
                cs.EndChunk();

                cs.BeginChunk(0x00);
                cs.Write(new byte[0xC]);
                cs.EndChunk();

                cs.BeginChunk(0x33320002);

                for (var i = 0; i < 0x78; i++)
                {
                    cs.Write((byte)0x11);
                }

                for (var i = 0; i < texturePack.Textures.Count; i++)
                {
                    var texture = texturePack.Textures[i];
                    cs.Write(texture.Data);

                    if (i != texturePack.Textures.Count - 1)
                    {
                        if (texture.Data.Length % 128 != 0)
                        {
                            var padding = 128 - texture.Data.Length % 128;

                            if (padding < 48)
                            {
                                padding = 48;
                            }

                            cs.Write(new byte[padding]);
                        }
                    }
                }

                cs.EndChunk();

                cs.EndChunk();
            }

            cs.EndChunk();
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
                                _texturePack.Textures = new List<Texture>((int)(chunkSize / 8));
                                break;
                            }
                        case 0x33310003:
                        {
                            _compressed = true;
                                while (br.BaseStream.Position < chunkEndPos)
                                {
                                    var tch = br.GetStruct<DataOffsetStruct>();
                                    var curPos = br.BaseStream.Position;

                                    br.BaseStream.Position = tch.Offset;

                                    var bytesRead = 0u;
                                    var blocks = new List<byte[]>();

                                    while (bytesRead < tch.LengthCompressed)
                                    {
                                        var compHeader = BinaryUtil.ReadStruct<Compression.CompressBlockHead>(br);
                                        var compressedData = br.ReadBytes((int)(compHeader.TotalBlockSize - 24));
                                        var outData = new byte[compHeader.OutSize];

                                        Array.Clear(outData, 0, outData.Length);

                                        if (BitConverter.ToUInt32(compressedData, 0) == 0x5a4c444a)
                                        {
                                            outData = JDLZ.Decompress(compressedData);
                                        }
                                        else
                                        {
                                            Compression.Decompress(compressedData, outData);
                                        }

                                        blocks.Add(outData);

                                        bytesRead += compHeader.TotalBlockSize;
                                    }

                                    // 1 block = end - 212
                                    // 2+ blocks = blocks[count - 2] -> end - 212

                                    if (blocks.Count == 1)
                                    {
                                        using (var mbr = new BinaryReader(new MemoryStream(blocks[0])))
                                        {
                                            mbr.BaseStream.Seek(-212, SeekOrigin.End);
                                            ReadTexture(mbr);
                                            mbr.BaseStream.Seek(-20, SeekOrigin.End);

                                            _texturePack.Textures[_texturePack.NumTextures - 1].CompressionType = (TextureCompression)mbr.ReadUInt32();
                                            Array.Clear(_texturePack.Textures[_texturePack.NumTextures - 1].Data, 0, _texturePack.Textures[_texturePack.NumTextures - 1].Data.Length);
                                            mbr.BaseStream.Position = 0;

                                            _texturePack.Textures[_texturePack.NumTextures - 1].Data = mbr.ReadBytes((uint)(mbr.BaseStream.Length - 212));
                                        }
                                    }
                                    else
                                    {
                                        var infoBlock = blocks[blocks.Count - 2];

                                        using (var mbr = new BinaryReader(new MemoryStream(infoBlock)))
                                        {
                                            ReadTexture(mbr);
                                            mbr.BaseStream.Seek(-20, SeekOrigin.End);

                                            _texturePack.Textures[_texturePack.NumTextures - 1].CompressionType = (TextureCompression)mbr.ReadUInt32();
                                        }

                                        _texturePack.Textures[_texturePack.NumTextures - 1].DataSize = 0;

                                        blocks.RemoveAt(blocks.Count - 2);

                                        _texturePack.Textures[_texturePack.NumTextures - 1].Data =
                                            blocks.SelectMany(b => b).ToArray();
                                        _texturePack.Textures[_texturePack.NumTextures - 1].DataSize =
                                            (uint) blocks.SelectMany(b => b).Count();
                                    }

                                    br.BaseStream.Position = curPos;
                                }

                                break;
                            }
                        case TexChunkId:
                            {
                                for (var j = 0; j < _texturePack.Textures.Capacity; j++)
                                {
                                    ReadTexture(br);
                                }

                                break;
                            }
                        case TexDataChunkId:
                            {
                                if (!_compressed)
                                {
                                    br.BaseStream.Position += 0x78;

                                    var basePos = br.BaseStream.Position;

                                    foreach (var t in _texturePack.Textures)
                                    {
                                        br.BaseStream.Position = basePos + t.DataOffset;
                                        br.Read(t.Data, 0, t.Data.Length);
                                    }
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

                                    t.CompressionType = (TextureCompression)compType;

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

        private Texture ReadTexture(BinaryReader br)
        {
            var texture = BinaryUtil.ReadStruct<TextureStruct>(br);
            var name = new string(br.ReadChars(texture.NameLength)).TrimEnd('\0');

            var realTexture = new Texture
            {
                Width = texture.Width,
                Height = texture.Height,
                Name = name,
                Data = new byte[texture.Unknown2],
                DataSize = (uint) texture.Unknown2,
                DataOffset = texture.DataOffset,
                MipMapCount = texture.MipMapCount,
                TexHash = texture.TexHash,
                TypeHash = texture.TypeHash,
                CompressionType = TextureCompression.Unknown,
                PitchOrLinearSize = (uint)texture.Unknown6,
                NameLength = texture.NameLength
            };

            _texturePack.Textures.Add(realTexture);

            Array.Clear(realTexture.Data, 0, realTexture.Data.Length);

            return realTexture;
        }
    }
}
