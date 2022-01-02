using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Common.Textures.Data;

namespace Common.Textures
{
    /// <summary>
    /// TPK support for NFS:UG2 and NFS:MW.
    /// </summary>
    public class Version2Tpk : TpkManager
    {
        private const uint InfoChunkId = 0x33310001;
        private const uint HashChunkId = 0x33310002;
        private const uint TexChunkId = 0x33310004;
        private const uint DDSChunkId = 0x33310005;
        private const uint TexDataChunkId = 0x33320002;
        private bool _compressed = false;

        private uint _offset, _size;

        private TexturePack _texturePack;

        public override TexturePack ReadTexturePack(BinaryReader br, uint containerSize)
        {
            _texturePack = new TexturePack();
            //_offset = (uint)(br.BaseStream.Position - 8);

            if (br.BaseStream.Position < 8)
                _offset = 0;
            else
                _offset = (uint)(br.BaseStream.Position - 8);

            _size = containerSize + 8;

            ReadChunks(br, containerSize);

            return _texturePack;
        }

        public virtual void WriteTexturePack(ChunkStream cs, TexturePack texturePack)
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
                        case 0x33310003:
                        {
                            _compressed = true;
                            Debug.Assert(chunkSize % Marshal.SizeOf<DataOffsetStruct>() == 0,
                                "chunkSize % Marshal.SizeOf<DataOffsetStruct>() == 0");

                            while (br.BaseStream.Position < chunkEndPos)
                            {
                                var dos = BinaryUtil.ReadStruct<DataOffsetStruct>(br);
                                var curPos = br.BaseStream.Position;

                                br.BaseStream.Position = dos.Offset;

                                ReadCompressedData(br, dos);

                                br.BaseStream.Position = curPos;
                            }

                            break;
                        }
                        case TexChunkId:
                        {
                            var textureStructSize = Marshal.SizeOf<TextureStruct>();
                            Debug.Assert(chunkSize % textureStructSize == 0);

                            var numTextures = chunkSize / textureStructSize;

                            for (var j = 0; j < numTextures; j++)
                            {
                                ReadTexture(br);
                            }

                            break;
                        }
                        case TexDataChunkId:
                        {
                            if (!_compressed)
                            {
                                BinaryUtil.AlignReader(br, 0x80);

                                var basePos = br.BaseStream.Position;

                                foreach (var t in _texturePack.Textures)
                                {
                                    br.BaseStream.Position = basePos + t.DataOffset;
                                    t.Data = new byte[t.DataSize];
                                    br.Read(t.Data, 0, t.Data.Length);
                                    if (t.PaletteSize > 0)
                                    {
                                        br.BaseStream.Position = basePos + t.PaletteOffset;
                                        t.Palette = new byte[t.PaletteSize];
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
                                br.BaseStream.Seek(20, SeekOrigin.Current);
                                t.Format = br.ReadUInt32();
                                //Console.WriteLine($"{t.Name} = 0x{((int)t.Format):X8}");
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
            var textureInfo = BinaryUtil.ReadStruct<TextureStruct>(br);

            var texture = new Texture
            {
                Width = textureInfo.Width,
                Height = textureInfo.Height,
                Name = textureInfo.DebugName,
                DataSize = textureInfo.ImageSize,
                DataOffset = textureInfo.ImagePlacement,
                PaletteSize = textureInfo.PaletteSize,
                PaletteOffset = textureInfo.PalettePlacement,
                MipMapCount = textureInfo.NumMipMapLevels,
                TexHash = textureInfo.NameHash,
                TypeHash = textureInfo.ClassNameHash,
                Format = 0,
                PitchOrLinearSize = textureInfo.BaseImageSize,
                CompressionType = textureInfo.ImageCompressionType,
                Data = new byte[textureInfo.ImageSize]
            };
            _texturePack.Textures.Add(texture);
            return texture;
        }

        private void ReadCompressedData(BinaryReader br, DataOffsetStruct dos)
        {
            var inData = new byte[dos.LengthCompressed];

            if (br.Read(inData, 0, inData.Length) != inData.Length)
                throw new Exception($"Failed to read compressed data for texture: 0x{dos.Hash:X8}");

            var outData = Compression.Decompress(inData).ToArray();

            using (var dcr = new BinaryReader(new MemoryStream(outData)))
            {
                // Seek to TextureInfo
                dcr.BaseStream.Seek(-156, SeekOrigin.End);
                var texture = ReadTexture(dcr);

                // Seek to D3DFormat
                dcr.BaseStream.Seek(-12, SeekOrigin.End);
                texture.Format = dcr.ReadUInt32();

                // Seek to data
                dcr.BaseStream.Seek(0, SeekOrigin.Begin);
                if (dcr.BaseStream.Read(texture.Data, 0, texture.Data.Length) !=
                    texture.Data.Length)
                    throw new Exception(
                        $"Failed to read data for texture 0x{texture.TexHash:X8} ({texture.Name})");
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 124)]
        private struct TextureStruct
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
            public readonly byte[] Padding1;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 24)]
            public readonly string DebugName;

            public readonly uint NameHash;
            public uint ClassNameHash;
            public readonly uint ImageParentHash;
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
            public readonly byte UsedFlag;
            public byte ApplyAlphaSorting;
            public byte AlphaUsageType;
            public byte AlphaBlendType;
            public byte Flags;
            public short ScrollTimeStep;
            public short ScrollSpeedS;
            public short ScrollSpeedT;
            public short OffsetS;
            public short OffsetT;
            public short ScaleS;
            public short ScaleT;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            public readonly byte[] Padding2;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct DataOffsetStruct
        {
            public uint Hash;
            public uint Offset;
            public readonly uint LengthCompressed;
            public readonly uint Length;
            public uint Flags;
            private uint blank;
        }
    }
}