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
    /// TPK support for NFS:UC
    /// </summary>
    public class UndercoverTpk : TpkManager
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
            public uint ImagePlacement;
            public uint PalettePlacement;
            public uint ImageSize;
            public uint PaletteSize;
            public uint BaseImageSize;
            public ushort Width;
            public ushort Height;
            public byte ShiftWidth;
            public byte ShiftHeight;
            public TextureCompressionType ImageCompressionType;
            public byte PaletteCompressionType;
            public ushort NumPaletteEntries;
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
            public byte Padding;
            public short ScrollTimeStep;
            public short ScrollSpeedS;
            public short ScrollSpeedT;
            public short OffsetS;
            public short OffsetT;
            public short ScaleS;
            public short ScaleT;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
            public byte[] Padding2;
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

                                    var bytesRead = 0;
                                    var decompressedData = new byte[tch.Length];

                                    while (bytesRead < tch.LengthCompressed)
                                    {
                                        var compHeader = BinaryUtil.ReadStruct<Compression.CompressBlockHead>(br);
                                        var compressedData = br.ReadBytes(compHeader.CSize - 24);
                                        var outData = new byte[compHeader.USize];
                                        Compression.Decompress(compressedData, outData);
                                        bytesRead += compHeader.CSize;
                                        Array.ConstrainedCopy(outData, 0, decompressedData, compHeader.UPos, outData.Length);
                                    }
                                    
                                    // Load decompressed texture
                                    using (var dcr = new BinaryReader(new MemoryStream(decompressedData)))
                                    {
                                        var texture = ReadTexture(dcr);
                                        // Seek to D3DFormat
                                        dcr.BaseStream.Seek(0x88, SeekOrigin.Begin);
                                        texture.Format = dcr.ReadUInt32();
                                        // Read texture data
                                        dcr.BaseStream.Seek(0x100, SeekOrigin.Begin);
                                        if (dcr.BaseStream.Read(texture.Data, 0, texture.Data.Length) !=
                                            texture.Data.Length)
                                        {
                                            throw new Exception(
                                                $"Failed to read data for texture 0x{texture.TexHash:X8} ({texture.Name})");
                                        }
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
                                        if (t.PaletteSize > 0)
                                        {
                                            br.BaseStream.Position = basePos + t.PaletteOffset;
                                            br.Read(t.Palette, 0, t.Palette.Length);
                                        }
                                    }
                                }

                                break;
                            }
                        case DDSChunkId:
                            {
                                foreach (var t in _texturePack.Textures)
                                {
                                    br.BaseStream.Seek(0x0C, SeekOrigin.Current);
                                    t.Format = br.ReadUInt32();
                                    br.BaseStream.Seek(0x08, SeekOrigin.Current);
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
                Data = new byte[texture.ImageSize],
                DataSize = texture.ImageSize,
                DataOffset = texture.ImagePlacement,
                MipMapCount = texture.NumMipMapLevels,
                TexHash = texture.NameHash,
                TypeHash = texture.ClassNameHash,
                Format = 0,
                PitchOrLinearSize = texture.BaseImageSize,
                CompressionType = texture.ImageCompressionType,
                PaletteOffset = texture.PalettePlacement,
                Palette = new byte[texture.PaletteSize],
                PaletteSize = texture.PaletteSize
            });

            return _texturePack.Textures[_texturePack.Textures.Count - 1];
        }
    }
}
