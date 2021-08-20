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

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct TextureStruct
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
            private byte[] blank;

            public uint NameHash;
            public uint ClassNameHash;
            public uint SurfaceNameHash;
            public uint ImageSize;
            public uint BaseImageSize;
            public uint Width;
            public uint Height;
            public uint ShiftWidth;
            public uint ShiftHeight;
            public TextureCompressionType ImageCompressionType;
            public byte NumMipMapLevels;
            public byte TilableUV;
            public byte BiasLevel;
            public byte RenderingOrder;
            public byte ScrollType;
            public byte UsedFlag;
            public byte ApplyAlphaSorting;
            public byte AlphaUsageType;
            public byte AlphaBlendType;
            public byte Flags;
            public byte MipmapBiasType;
            public short ScrollTimeStep;
            public short ScrollSpeedS;
            public short ScrollSpeedT;
            public short OffsetS;
            public short OffsetT;
            public short ScaleS;
            public short ScaleT;
            public short Pad;
            public uint DataSize0;
            public uint DataOffset0;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1 + 3*4 + 2)]
            public uint[] Padding;

            public byte DebugNameSize;
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
            throw new NotImplementedException();
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

                                            _texturePack.Textures[_texturePack.Textures.Count - 1].Format = mbr.ReadUInt32();
                                            Array.Clear(_texturePack.Textures[_texturePack.Textures.Count - 1].Data, 0, _texturePack.Textures[_texturePack.Textures.Count - 1].Data.Length);
                                            mbr.BaseStream.Position = 0;

                                            _texturePack.Textures[_texturePack.Textures.Count - 1].Data = mbr.ReadBytes((uint)(mbr.BaseStream.Length - 212));
                                        }
                                    }
                                    else
                                    {
                                        var infoBlock = blocks[blocks.Count - 2];

                                        using (var mbr = new BinaryReader(new MemoryStream(infoBlock)))
                                        {
                                            ReadTexture(mbr);
                                            mbr.BaseStream.Seek(-20, SeekOrigin.End);

                                            _texturePack.Textures[_texturePack.Textures.Count - 1].Format = mbr.ReadUInt32();
                                        }

                                        _texturePack.Textures[_texturePack.Textures.Count - 1].DataSize = 0;

                                        blocks.RemoveAt(blocks.Count - 2);

                                        _texturePack.Textures[_texturePack.Textures.Count - 1].Data =
                                            blocks.SelectMany(b => b).ToArray();
                                        _texturePack.Textures[_texturePack.Textures.Count - 1].DataSize =
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

                                    t.Format = compType;

                                    //t.Format = (TextureFormat)br.ReadUInt32();
                                    //Console.WriteLine($"{t.Name} = 0x{((int)t.Format):X8}");
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
            var name = new string(br.ReadChars(texture.DebugNameSize)).TrimEnd('\0');

            var realTexture = new Texture
            {
                Width = texture.Width,
                Height = texture.Height,
                Name = name,
                Data = new byte[texture.ImageSize],
                DataSize = texture.DataSize0,
                DataOffset = texture.DataOffset0,
                MipMapCount = texture.NumMipMapLevels,
                TexHash = texture.NameHash,
                TypeHash = texture.ClassNameHash,
                Format = 0,
                PitchOrLinearSize = texture.BaseImageSize,
                CompressionType = texture.ImageCompressionType
            };

            _texturePack.Textures.Add(realTexture);

            Array.Clear(realTexture.Data, 0, realTexture.Data.Length);

            return realTexture;
        }
    }
}
