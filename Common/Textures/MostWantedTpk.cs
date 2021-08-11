using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Common.Textures.Data;

namespace Common.Textures
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
            public byte[] Padding1;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 24)]
            public string DebugName;

            public uint NameHash;
            public uint ClassNameHash;
            public uint ImageParentHash;
            public uint ImagePlacement;
            public uint PalettePlacement;
            public uint ImageSize;
            public uint PaletteSize;
            public uint BaseImageSize;
            public ushort Width;
            public ushort Height;
            public byte ShiftWidth;
            public byte ShiftHeight;
            public byte ImageCompressionType;
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
            public short ScrollTimeStep;
            public short ScrollSpeedS;
            public short ScrollSpeedT;
            public short OffsetS;
            public short OffsetT;
            public short ScaleS;
            public short ScaleT;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            public byte[] Padding2;

            //public uint Hash;

            //public uint Type;

            //private int blank2;

            //public uint DataOffset;

            //public uint PaletteOffset;

            //public uint DataSize, PaletteSize, PitchOrLinearSize;

            //public ushort Width, Height;

            //public uint D1;

            //public ushort D2;

            //public byte D3; // mipmap?
            //public byte D4;

            //[MarshalAs(UnmanagedType.ByValArray, SizeConst = 44)]
            //public byte[] RestOfData;
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
            //_offset = (uint)(br.BaseStream.Position - 8);

            if (br.BaseStream.Position < 8)
            {
                _offset = 0;
            }
            else
            {
                _offset = (uint)(br.BaseStream.Position - 8);
            }

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
                        case 0x33310003:
                            {
                                _compressed = true;
                                Debug.Assert(chunkSize % Marshal.SizeOf<DataOffsetStruct>() == 0, "chunkSize % Marshal.SizeOf<DataOffsetStruct>() == 0");

                                while (br.BaseStream.Position < chunkEndPos)
                                {
                                    var dos = br.GetStruct<DataOffsetStruct>();
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
                                    BinaryUtil.AutoAlign(br, 0x80);

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

        private void ReadTexture(BinaryReader br)
        {
            var texture = BinaryUtil.ReadStruct<TextureStruct>(br);

            _texturePack.Textures.Add(new Texture
            {
                Width = texture.Width,
                Height = texture.Height,
                Name = texture.DebugName,
                DataSize = texture.ImageSize,
                DataOffset = texture.ImagePlacement,
                PaletteSize = texture.PaletteSize,
                PaletteOffset = texture.PalettePlacement,
                MipMapCount = texture.NumMipMapLevels,
                TexHash = texture.NameHash,
                TypeHash = texture.ClassNameHash,
                Format = 0,
                PitchOrLinearSize = texture.BaseImageSize
            });
        }

        private void ReadCompressedData(BinaryReader br, DataOffsetStruct dos)
        {
            var compHead = br.GetStruct<Compression.SimpleCompressionHeader>();
            br.BaseStream.Position -= 16;

            var inData = new byte[dos.LengthCompressed];
            var outData = new byte[compHead.OutLength];

            br.Read(inData, 0, inData.Length);

            Compression.Decompress(inData, outData);

            using (var ms = new MemoryStream(outData))
            {
                ms.Position = ms.Length - 156;

                using (var cbr = new BinaryReader(ms))
                {
                    ReadTexture(cbr);
                }
            }

            _texturePack.Textures[_texturePack.NumTextures - 1].Data = new byte[outData.Length - 156];

            Array.ConstrainedCopy(outData, 0, _texturePack.Textures[_texturePack.NumTextures - 1].Data, 0,
                outData.Length - 156);

            _texturePack.Textures[_texturePack.NumTextures - 1].Format = BitConverter.ToUInt32(outData, outData.Length - 12);
        }
    }
}
