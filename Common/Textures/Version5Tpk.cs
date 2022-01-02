using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Common.Textures.Data;

namespace Common.Textures;

/// <summary>
///     TPK support for NFS:W.
/// </summary>
public class Version5Tpk : TpkManager
{
    private const uint InfoChunkId = 0x33310001;
    private const uint HashChunkId = 0x33310002;
    private const uint TexChunkId = 0x33310004;
    private const uint DDSChunkId = 0x33310005;
    private const uint TexDataChunkId = 0x33320002;
    private bool _compressed;

    private uint _offset, _size;

    private TexturePack _texturePack;

    public override TexturePack ReadTexturePack(BinaryReader br, uint containerSize)
    {
        _texturePack = new TexturePack();
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
                ReadChunks(br, chunkSize);
            else
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
                            var tch = BinaryUtil.ReadStruct<DataOffsetStruct>(br);
                            var curPos = br.BaseStream.Position;

                            br.BaseStream.Position = tch.Offset;

                            // Load decompressed texture
                            using (var ms = new MemoryStream())
                            {
                                Compression.DecompressCip(br.BaseStream, ms, tch.LengthCompressed);

                                using (var dcr = new BinaryReader(ms))
                                {
                                    // Seek to TextureInfo
                                    dcr.BaseStream.Seek(-0xD4, SeekOrigin.End);
                                    var texture = ReadTexture(dcr);
                                    // Seek to D3DFormat
                                    dcr.BaseStream.Seek(-0x14, SeekOrigin.End);
                                    texture.Format = dcr.ReadUInt32();
                                    // Read texture data
                                    dcr.BaseStream.Seek(0, SeekOrigin.Begin);
                                    if (dcr.BaseStream.Read(texture.Data, 0, texture.Data.Length) !=
                                        texture.Data.Length)
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
                        for (var j = 0; j < _texturePack.Textures.Capacity; j++) ReadTexture(br);

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

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct TextureStruct
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        private readonly byte[] blank;

        public readonly uint NameHash;
        public readonly uint ClassNameHash;
        public readonly uint SurfaceNameHash;
        public readonly uint ImageSize;
        public readonly uint BaseImageSize;
        public readonly uint Width;
        public readonly uint Height;
        public readonly uint ShiftWidth;
        public readonly uint ShiftHeight;
        public readonly TextureCompressionType ImageCompressionType;
        public readonly byte NumMipMapLevels;
        public readonly byte TilableUV;
        public readonly byte BiasLevel;
        public readonly byte RenderingOrder;
        public readonly byte ScrollType;
        public readonly byte UsedFlag;
        public readonly byte ApplyAlphaSorting;
        public readonly byte AlphaUsageType;
        public readonly byte AlphaBlendType;
        public readonly byte Flags;
        public readonly byte MipmapBiasType;
        public readonly short ScrollTimeStep;
        public readonly short ScrollSpeedS;
        public readonly short ScrollSpeedT;
        public readonly short OffsetS;
        public readonly short OffsetT;
        public readonly short ScaleS;
        public readonly short ScaleT;
        public readonly short Pad;
        public readonly uint DataSize0;
        public readonly uint DataOffset0;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1 + 3 * 4 + 2)]
        public readonly uint[] Padding;

        public readonly byte DebugNameSize;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct DataOffsetStruct
    {
        public readonly uint Hash;
        public readonly uint Offset;
        public readonly int LengthCompressed;
        public readonly int Length;
        public readonly uint Flags;
        private readonly uint blank;
        private readonly uint blank2;
        private readonly uint blank3;
        private readonly uint blank4;
    }
}