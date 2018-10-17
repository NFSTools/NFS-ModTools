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
                                    br.BaseStream.Position += 0x78;

                                    var basePos = br.BaseStream.Position;

                                    foreach (var t in _texturePack.Textures)
                                    {
                                        br.BaseStream.Position = basePos + t.DataOffset;
                                        t.Data = new byte[t.DataSize];

                                        br.Read(t.Data, 0, t.Data.Length);
                                    }
                                }

                                break;
                            }
                        case DDSChunkId:
                            {
                                foreach (var t in _texturePack.Textures)
                                {
                                    br.BaseStream.Seek(20, SeekOrigin.Current);
                                    t.CompressionType = (TextureCompression)br.ReadUInt32();
                                    //Console.WriteLine($"{t.Name} = 0x{((int)t.CompressionType):X8}");
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
                Name = texture.Name,
                DataSize = texture.DataSize,
                DataOffset = texture.DataOffset,
                MipMapCount = texture.D3,
                TexHash = texture.Hash,
                TypeHash = texture.Type,
                CompressionType = TextureCompression.Unknown,
                PitchOrLinearSize = texture.PitchOrLinearSize
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

            _texturePack.Textures[_texturePack.NumTextures - 1].CompressionType = (TextureCompression)BitConverter.ToUInt32(outData, outData.Length - 12);

            using (var fs = File.OpenWrite($"{dos.Hash:X8}.dds"))
            {
                var ddsHeader = new DDSHeader();
                var texture = _texturePack.Textures[_texturePack.NumTextures - 1];
                ddsHeader.Init(texture);

                using (var bw = new BinaryWriter(fs))
                {
                    BinaryUtil.WriteStruct(bw, ddsHeader);
                    bw.Write(texture.Data, 0, texture.Data.Length);
                }
            }
        }
    }
}
