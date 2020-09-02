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
    /// TPK support for NFS:C, NFS:PS, NFS:UC(?)
    /// </summary>
    public class CarbonTpk : TpkManager
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

            public uint Hash;

            public uint Type;

            public uint DataOffset;

            public uint Unk1;

            public uint DataSize;

            public uint Unk2;

            public uint Scaler;

            public ushort Width;

            public ushort Height;

            public byte MipMapCount;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public byte[] Unk3;

            public uint Flags;

            public uint Unk4;
            public uint Unk5;
            public uint Unk6;
            public uint Unk7;
            public uint Unk8;
            public uint Unk9;
            public uint Unk10;
            public uint Unk11;
            public uint Unk12;
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

                                        Compression.Decompress(compressedData, outData);

                                        blocks.Add(outData);

                                        bytesRead += compHeader.TotalBlockSize;
                                    }

                                    //using (var fs = File.OpenWrite($"debugtex_{tch.Hash:X8}.bin"))
                                    //{
                                    //    for (var index = 0; index < blocks.Count; index++)
                                    //    {
                                    //        var block = blocks[index];
                                    //        var blockHeader = $"BEGIN BLOCK #{index + 1}/{blocks.Count}";
                                    //        fs.Write(Encoding.ASCII.GetBytes(blockHeader), 0, blockHeader.Length);

                                    //        var align = (int) (0x10 - fs.Position % 0x10);

                                    //        if (align != 0x10)
                                    //        {
                                    //            fs.Write(new byte[align], 0, align);
                                    //        }

                                    //        fs.Write(block, 0, block.Length);

                                    //        blockHeader = $"END BLOCK #{index + 1}/{blocks.Count}";
                                    //        fs.Write(Encoding.ASCII.GetBytes(blockHeader), 0, blockHeader.Length);

                                    //        align = (int) (0x10 - fs.Position % 0x10);

                                    //        if (align != 0x10)
                                    //        {
                                    //            fs.Write(new byte[align], 0, align);
                                    //        }
                                    //    }
                                    //}

                                    if (blocks.Count == 1)
                                    {
                                        using (var ms = new MemoryStream(blocks[0]))
                                        using (var mbr = new BinaryReader(ms))
                                        {
                                            mbr.BaseStream.Seek(-148, SeekOrigin.End);
                                            var texture = ReadTexture(mbr);

                                            mbr.BaseStream.Seek(-12, SeekOrigin.End);
                                            texture.CompressionType = (TextureCompression)mbr.ReadUInt32();

                                            Array.ConstrainedCopy(blocks[0], 0, texture.Data, 0, texture.Data.Length);
                                        }
                                    }
                                    else if (blocks.Count > 1)
                                    {
                                        // Sort the blocks into their proper order.
                                        Texture texture;

                                        using (var mbr = new BinaryReader(new MemoryStream(blocks[blocks.Count - 2])))
                                        {
                                            texture = ReadTexture(mbr);

                                            mbr.BaseStream.Seek(-12, SeekOrigin.End);
                                            texture.CompressionType = (TextureCompression)mbr.ReadUInt32();
                                        }

                                        var copiedDataBytes = 0;

                                        for (var j = 0; j < blocks.Count; j++)
                                        {
                                            if (j != blocks.Count - 2)
                                            {
                                                Array.ConstrainedCopy(blocks[j], 0, texture.Data, copiedDataBytes, blocks[j].Length);

                                                copiedDataBytes += blocks[j].Length;
                                            }
                                        }
                                    }

                                    blocks.Clear();

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
                                if (chunkSize / _texturePack.NumTextures == 0x18)
                                {
                                    foreach (var t in _texturePack.Textures)
                                    {
                                        br.BaseStream.Seek(0x0C, SeekOrigin.Current);
                                        t.CompressionType = (TextureCompression)br.ReadUInt32();
                                        br.BaseStream.Seek(0x08, SeekOrigin.Current);
                                    }
                                }
                                else
                                {
                                    foreach (var t in _texturePack.Textures)
                                    {
                                        var data = br.ReadBytesRequired(0x2C);

                                        Console.WriteLine(BinaryUtil.HexDump(data));
                                    }
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
            var nameLength = br.ReadByte();

            var name = new string(br.ReadChars(nameLength)).TrimEnd('\0');

            _texturePack.Textures.Add(new Texture
            {
                Width = texture.Width,
                Height = texture.Height,
                Name = name,
                Data = new byte[texture.DataSize],
                DataSize = texture.DataSize,
                DataOffset = texture.DataOffset,
                MipMapCount = texture.MipMapCount,
                TexHash = texture.Hash,
                TypeHash = texture.Type,
                CompressionType = TextureCompression.Unknown,
                PitchOrLinearSize = texture.DataSize,
                NameLength = nameLength
            });

            return _texturePack.Textures[_texturePack.NumTextures - 1];
        }
    }
}
