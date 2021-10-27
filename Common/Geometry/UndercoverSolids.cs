using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Common.Geometry.Data;

namespace Common.Geometry
{
    public class UndercoverSolids : SolidListManager
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct SolidListInfo
        {
            public readonly long Blank;

            public readonly int Marker; // this doesn't change between games for some reason... rather unfortunate

            public readonly int NumObjects;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x38)]
            public readonly string PipelinePath;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
            public readonly string ClassType;

            public readonly int UnknownOffset;
            public readonly int UnknownSize;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct SolidObjectHeader
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            private readonly byte[] zero;

            public readonly uint ObjectHash;

            public readonly uint NumTris;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public readonly byte[] pad2;

            public readonly Vector3 BoundsMin;
            public readonly int Blank5;

            public readonly Vector3 BoundsMax;
            public readonly int Blank6;

            public readonly Matrix4x4 Transform;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public readonly uint[] unknown;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
            public readonly byte[] blank;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct SolidObjectDescriptor
        {
            public readonly long Unknown1;

            public readonly uint Unknown2;

            public readonly uint Flags;

            public readonly uint MaterialShaderNum;

            public readonly uint Zero;

            public readonly uint NumVertexStreams; // should be 0, # of materials = # of streams

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
            public readonly uint[] Zeroes;

            public readonly uint NumTris;
            public readonly uint NumTriIdx;
            public readonly uint NumReducedTris;
            public readonly uint NumReducedTriIdx;

            public readonly ushort NumVerts;
            public readonly ushort Unknown3;

            public readonly uint Zero3;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 40)]
        internal struct eVertexStream
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public uint[] VertexBufferOpaque; // @0x0

            public uint SizeOfVertexData; // @0x20
            public byte Stream; // @0x24
            public byte LoadStreamIndex; // @0x25
            public short VertexBytesAndFlag; // @0x26
            // end: 0x28
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct Material
        {
            public readonly int FirstIndex; // @0x0
            public readonly int LastIndex; // @0x4
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
            public readonly int[] TextureParam; // @0x8
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
            public readonly byte[] TextureNumber; // @0x30

            public readonly byte NumTextures; // @0x3A
            public readonly byte LightMaterialNumber; // @0x3B

            public readonly uint TextureSortKey; // @0x3C
            public readonly uint MeshFlags; // @0x40
            public readonly int IdxUsed; // @0x44

            public readonly uint NumReducedIdx;
            public eVertexStream VertexStream; // @0x48

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public readonly int[] Zero4; // @0x74

            public uint MaterialAttribKey; // @0x84
            public uint MaterialAttribPointer; // @0x88

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
            public readonly int[] TextureParamMaterial; // @0x8C

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
            public readonly uint[] TextureNameMaterial; // @0xB4

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 36)]
            public readonly byte[] Zero5; // @0xDC

            // end: 0x100
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 0x18)]
        private struct SolidObjectOffset
        {
            public uint ObjectHash;
            public uint Offset;
            public uint CompressedSize;
            public uint OutSize;
            public uint Unknown1;
            public uint Unknown2;
        }

        private const uint SolidListInfoChunk = 0x134002;
        private const uint SolidListObjHeadChunk = 0x134011;

        private SolidList _solidList;

        private int _namedMaterials;

        private static Dictionary<uint, UndercoverObject.EffectID> _effectIdMapping = new Dictionary<uint, UndercoverObject.EffectID>
        {
            { 0x01747d6f, UndercoverObject.EffectID.mw2_diffuse_spec },
            { 0x029546b2, UndercoverObject.EffectID.ubereffectblend },
            { 0x05ff7435, UndercoverObject.EffectID.standardeffect },
            { 0x06b7ad24, UndercoverObject.EffectID.watersplash },
            { 0x073b344f, UndercoverObject.EffectID.mw2_ocean },
            { 0x09f8b274, UndercoverObject.EffectID.mw2_glass_refl },
            { 0x0a19d914, UndercoverObject.EffectID.mw2_glass_no_n },
            { 0x0ae016e3, UndercoverObject.EffectID.worldbone },
            { 0x0cad599e, UndercoverObject.EffectID.mw2_tunnel_road },
            { 0x0ddd6c5c, UndercoverObject.EffectID.car_si_a },
            { 0x0f87f3b6, UndercoverObject.EffectID.worldbonetransparency },
            { 0x117b614f, UndercoverObject.EffectID.worldbone },
            { 0x13b4af33, UndercoverObject.EffectID.worldbone },
            { 0x170e6ebf, UndercoverObject.EffectID.mw2_car_heaven },
            { 0x18c7652d, UndercoverObject.EffectID.mw2_combo_refl },
            { 0x19729193, UndercoverObject.EffectID.mw2_road_lite },
            { 0x19b330d9, UndercoverObject.EffectID.mw2_cardebris },
            { 0x1ad21fd4, UndercoverObject.EffectID.mw2_road_refl_lite },
            { 0x1b801884, UndercoverObject.EffectID.mw2_scrub_lod },
            { 0x1ecc6c4e, UndercoverObject.EffectID.mw2_car_heaven_default },
            { 0x1f264952, UndercoverObject.EffectID.worldbone },
            { 0x2466ee21, UndercoverObject.EffectID.standardeffect },
            { 0x24be0f6e, UndercoverObject.EffectID.mw2_road_refl_lite },
            { 0x266e34fe, UndercoverObject.EffectID.mw2_grass_dirt },
            { 0x275232ed, UndercoverObject.EffectID.diffuse_spec_2sided },
            { 0x2a872bf4, UndercoverObject.EffectID.worldbonenocull },
            { 0x2d3b0296, UndercoverObject.EffectID.mw2_road_refl },
            { 0x2eb62de1, UndercoverObject.EffectID.car_a },
            { 0x308cd669, UndercoverObject.EffectID.mw2_texture_scroll },
            { 0x3242d31a, UndercoverObject.EffectID.mw2_diffuse_spec },
            { 0x354f479d, UndercoverObject.EffectID.worldbonenocull },
            { 0x35aa265b, UndercoverObject.EffectID.mw2_road_refl_tile },
            { 0x361cbdfa, UndercoverObject.EffectID.standardeffect },
            { 0x3620b527, UndercoverObject.EffectID.mw2_icon },
            { 0x3655d45f, UndercoverObject.EffectID.mw2_normalmap },
            { 0x39ffaf87, UndercoverObject.EffectID.mw2_scrub },
            { 0x3d2276dc, UndercoverObject.EffectID.mw2_diffuse_spec_alpha },
            { 0x3fccbbf5, UndercoverObject.EffectID.car_nm },
            { 0x41fc3e7c, UndercoverObject.EffectID.mw2_combo_refl },
            { 0x42b42128, UndercoverObject.EffectID.mw2_matte },
            { 0x47816d49, UndercoverObject.EffectID.mw2_diffuse_spec_alpha },
            { 0x4c42e398, UndercoverObject.EffectID.worldbone },
            { 0x4c4b3f1d, UndercoverObject.EffectID.mw2_pano },
            { 0x4ce1dd43, UndercoverObject.EffectID.car_t_nm },
            { 0x4e479629, UndercoverObject.EffectID.mw2_rock },
            { 0x4f5f8200, UndercoverObject.EffectID.shadowmesh },
            { 0x54f5a555, UndercoverObject.EffectID.car_a_nzw },
            { 0x56a2b9d4, UndercoverObject.EffectID.mw2_normalmap },
            { 0x572610ed, UndercoverObject.EffectID.mw2_diffuse_spec },
            { 0x5797ec4b, UndercoverObject.EffectID.worldbone },
            { 0x5c759d61, UndercoverObject.EffectID.mw2_normalmap },
            { 0x5d5d135a, UndercoverObject.EffectID.mw2_illuminated },
            { 0x62b474b4, UndercoverObject.EffectID.mw2_diffuse_spec },
            { 0x645c60e6, UndercoverObject.EffectID.standardeffect },
            { 0x64b9573b, UndercoverObject.EffectID.mw2_tunnel_road },
            { 0x64f0aba2, UndercoverObject.EffectID.car_v },
            { 0x67a95199, UndercoverObject.EffectID.mw2_constant },
            { 0x698bb218, UndercoverObject.EffectID.mw2_road_refl_tile },
            { 0x6f837ecd, UndercoverObject.EffectID.mw2_constant_alpha_bias },
            { 0x705c7fa1, UndercoverObject.EffectID.normalmap2sided },
            { 0x7155733b, UndercoverObject.EffectID.ubereffect },
            { 0x71770ad7, UndercoverObject.EffectID.car_nm_a },
            { 0x7773651b, UndercoverObject.EffectID.mw2_diffuse_spec },
            { 0x7acc11b2, UndercoverObject.EffectID.mw2_road_refl_overlay },
            { 0x7b83a181, UndercoverObject.EffectID.mw2_constant },
            { 0x7dac986f, UndercoverObject.EffectID.mw2_road },
            { 0x841dd0aa, UndercoverObject.EffectID.mw2_tunnel_wall },
            { 0x87a1a883, UndercoverObject.EffectID.mw2_branches },
            { 0x89ad50b6, UndercoverObject.EffectID.mw2_sky },
            { 0x8a66ab20, UndercoverObject.EffectID.mw2_tunnel_illum },
            { 0x8ad4dfdc, UndercoverObject.EffectID.mw2_ocean },
            { 0x8e0d6a1b, UndercoverObject.EffectID.mw2_foliage },
            { 0x923f903c, UndercoverObject.EffectID.mw2_grass_rock },
            { 0x92510617, UndercoverObject.EffectID.ubereffect },
            { 0x9303b776, UndercoverObject.EffectID.worldbone },
            { 0x97745fa6, UndercoverObject.EffectID.mw2_road_tile },
            { 0x97875314, UndercoverObject.EffectID.mw2_road_lite },
            { 0x99f1e4f0, UndercoverObject.EffectID.mw2_matte_alpha },
            { 0x9c215ff1, UndercoverObject.EffectID.mw2_road_overlay },
            { 0x9d96c16f, UndercoverObject.EffectID.mw2_smokegeo },
            { 0x9db7d630, UndercoverObject.EffectID.mw2_texture_scroll },
            { 0xa063c459, UndercoverObject.EffectID.worldbone },
            { 0xa13753eb, UndercoverObject.EffectID.car },
            { 0xa5356b04, UndercoverObject.EffectID.mw2_matte_alpha },
            { 0xa554e057, UndercoverObject.EffectID.mw2_texture_scroll },
            { 0xa60c8560, UndercoverObject.EffectID.worldbone },
            { 0xa8a62dc5, UndercoverObject.EffectID.car_nm_v_s },
            { 0xab704872, UndercoverObject.EffectID.car_t_a },
            { 0xab7d3ce6, UndercoverObject.EffectID.worldbonetransparency },
            { 0xadc5a4c1, UndercoverObject.EffectID.mw2_normalmap_bias },
            { 0xafec11b7, UndercoverObject.EffectID.mw2_dirt },
            { 0xb25c1588, UndercoverObject.EffectID.car_si },
            { 0xb5fdc966, UndercoverObject.EffectID.worldbonetransparency },
            { 0xb8b7e7fd, UndercoverObject.EffectID.standardeffect },
            { 0xbbf81161, UndercoverObject.EffectID.mw2_trunk },
            { 0xbcd89ccc, UndercoverObject.EffectID.mw2_ocean },
            { 0xbfd62731, UndercoverObject.EffectID.mw2_road },
            { 0xc1368b6a, UndercoverObject.EffectID.car_nm_v_s_a },
            { 0xc146dcc6, UndercoverObject.EffectID.mw2_diffuse_spec_salpha },
            { 0xc7bf7382, UndercoverObject.EffectID.mw2_fol_alwaysfacing },
            { 0xc808bd33, UndercoverObject.EffectID.worldbonetransparency },
            { 0xcc72efc6, UndercoverObject.EffectID.mw2_road_refl },
            { 0xcca7bd31, UndercoverObject.EffectID.mw2_dif_spec_a_bias },
            { 0xd34aa470, UndercoverObject.EffectID.mw2_illuminated },
            { 0xd3865634, UndercoverObject.EffectID.mw2_rock_overlay },
            { 0xd3c214f5, UndercoverObject.EffectID.worldbonetransparency },
            { 0xd5289f76, UndercoverObject.EffectID.mw2_diffuse_spec_alpha },
            { 0xd5912485, UndercoverObject.EffectID.worldbone },
            { 0xd60bdb29, UndercoverObject.EffectID.mw2_road_refl_overlay },
            { 0xd9cd6dbb, UndercoverObject.EffectID.mw2_indicator },
            { 0xdc77b9a2, UndercoverObject.EffectID.mw2_carhvn_floor },
            { 0xe643efd6, UndercoverObject.EffectID.mw2_foliage_lod },
            { 0xe6e487ce, UndercoverObject.EffectID.mw2_dirt_rock },
            { 0xe8983e20, UndercoverObject.EffectID.mw2_diffuse_spec_illum },
            { 0xe9abeaa2, UndercoverObject.EffectID.worldbonetransparency },
            { 0xee3e7019, UndercoverObject.EffectID.mw2_dirt_overlay },
            { 0xf0598a3f, UndercoverObject.EffectID.car_t },
            { 0xf0b14117, UndercoverObject.EffectID.mw2_normalmap },
            { 0xf20a36b0, UndercoverObject.EffectID.mw2_grass },
            { 0xf2e4a987, UndercoverObject.EffectID.mw2_road_overlay },
            { 0xf2e790ae, UndercoverObject.EffectID.mw2_matte },
            { 0xf54fe578, UndercoverObject.EffectID.mw2_parallax },
            { 0xf642075f, UndercoverObject.EffectID.worldbone },
            { 0xf8105027, UndercoverObject.EffectID.mw2_diffuse_spec },
            { 0xfddf0f66, UndercoverObject.EffectID.mw2_road_tile },
            { 0xff37175c, UndercoverObject.EffectID.worldbonenocull },
        };

        public override SolidList ReadSolidList(BinaryReader br, uint containerSize)
        {
            _solidList = new SolidList();
            _namedMaterials = 0;

            ReadChunks(br, containerSize);

            return _solidList;
        }

        protected override void ReadChunks(BinaryReader br, uint containerSize)
        {
            var endPos = br.BaseStream.Position + containerSize;

            while (br.BaseStream.Position < endPos)
            {
                var chunkId = br.ReadUInt32();
                var chunkSize = br.ReadUInt32();
                var chunkEndPos = br.BaseStream.Position + chunkSize;

                if (chunkId == 0x55441122)
                {
                    break;
                }

                if ((chunkId & 0x80000000) == 0x80000000 && chunkId != 0x80134010)
                {
                    ReadChunks(br, chunkSize);
                }
                else
                {
                    //if ((chunkId & 0x80000000) != 0x80000000)
                    //{
                    //    var padding = 0u;

                    //    while (br.ReadByte() == 0x11)
                    //    {
                    //        padding++;
                    //    }

                    //    br.BaseStream.Position--;

                    //    if (padding % 2 != 0)
                    //    {
                    //        padding--;
                    //        br.BaseStream.Position--;
                    //    }

                    //    chunkSize -= padding;
                    //}

                    switch (chunkId)
                    {
                        case SolidListInfoChunk:
                            {
                                var info = BinaryUtil.ReadStruct<SolidListInfo>(br);

                                _solidList.ClassType = info.ClassType;
                                _solidList.PipelinePath = info.PipelinePath;
                                _solidList.ObjectCount = info.NumObjects;

                                break;
                            }
                        case 0x134004:
                            {
                                while (br.BaseStream.Position < chunkEndPos)
                                {
                                    var och = BinaryUtil.ReadStruct<SolidObjectOffset>(br);
                                    var curPos = br.BaseStream.Position;
                                    br.BaseStream.Position = och.Offset;

                                    if (och.CompressedSize == och.OutSize)
                                    {
                                        var data = br.ReadBytes(och.OutSize);

                                        using (var ms = new MemoryStream(data))
                                        using (var mbr = new BinaryReader(ms))
                                        {
                                            var solidObject = ReadObject(mbr, och.OutSize, true, null);
                                            solidObject.PostProcessing();

                                            _solidList.Objects.Add(solidObject);
                                        }
                                    }
                                    else
                                    {
                                        var bytesRead = 0u;
                                        var blocks = new List<byte[]>();

                                        while (bytesRead < och.CompressedSize)
                                        {
                                            var compHeader = BinaryUtil.ReadStruct<Compression.CompressBlockHead>(br);
                                            var compressedData = br.ReadBytes((int)(compHeader.TotalBlockSize - 24));
                                            var outData = new byte[compHeader.OutSize];

                                            Compression.Decompress(compressedData, outData);

                                            blocks.Add(outData);

                                            bytesRead += compHeader.TotalBlockSize;
                                        }

                                        if (blocks.Count == 1)
                                        {
                                            using (var ms = new MemoryStream(blocks[0]))
                                            using (var mbr = new BinaryReader(ms))
                                            {
                                                var solidObject = ReadObject(mbr, blocks[0].Length, true, null);
                                                solidObject.PostProcessing();

                                                _solidList.Objects.Add(solidObject);
                                            }
                                        }
                                        else if (blocks.Count > 1)
                                        {
                                            // Sort the blocks into their proper order.
                                            var sorted = new List<byte>();

                                            sorted.AddRange(blocks[blocks.Count - 1]);

                                            for (var j = 0; j < blocks.Count; j++)
                                            {
                                                if (j != blocks.Count - 1)
                                                {
                                                    sorted.AddRange(blocks[j]);
                                                }
                                            }

                                            using (var ms = new MemoryStream(sorted.ToArray()))
                                            using (var mbr = new BinaryReader(ms))
                                            {
                                                var solidObject = ReadObject(mbr, sorted.Count, true, null);
                                                solidObject.PostProcessing();

                                                _solidList.Objects.Add(solidObject);
                                            }

                                            sorted.Clear();
                                        }

                                        blocks.Clear();
                                    }

                                    br.BaseStream.Position = curPos;
                                }
                                break;
                            }
                        case 0x80134010:
                            {
                                var solidObject = ReadObject(br, chunkSize, false, null);
                                solidObject.PostProcessing();
                                _solidList.Objects.Add(solidObject);
                                break;
                            }
                        default:
                            //Console.WriteLine($"0x{chunkId:X8} [{chunkSize}] @{br.BaseStream.Position}");
                            break;
                    }
                }

                br.BaseStream.Position = chunkEndPos;
            }
        }

        public override void WriteSolidList(ChunkStream chunkStream, SolidList solidList)
        {
            throw new NotImplementedException();
        }

        private UndercoverObject ReadObject(BinaryReader br, long size, bool compressed, UndercoverObject solidObject)
        {
            if (solidObject == null)
                solidObject = new UndercoverObject();

            solidObject.IsCompressed = compressed;
            solidObject.RotationAngle = 90.0f;

            var endPos = br.BaseStream.Position + size;

            while (br.BaseStream.Position < endPos)
            {
                var chunkId = br.ReadUInt32();
                var chunkSize = br.ReadUInt32();
                var chunkEndPos = br.BaseStream.Position + chunkSize;

                if (chunkSize == 0) continue;

                if ((chunkId & 0x80000000) == 0x80000000)
                {
                    solidObject = ReadObject(br, chunkSize, compressed, solidObject);
                }
                else
                {
                    var padding = 0u;

                    while (br.ReadUInt32() == 0x11111111)
                    {
                        padding += 4;

                        if (br.BaseStream.Position >= chunkEndPos)
                        {
                            break;
                        }
                    }

                    br.BaseStream.Position -= 4;
                    chunkSize -= padding;

                    switch (chunkId)
                    {
                        case SolidListObjHeadChunk:
                            {
                                _namedMaterials = 0;

                                var header = BinaryUtil.ReadStruct<SolidObjectHeader>(br);
                                var name = BinaryUtil.ReadNullTerminatedString(br);

                                solidObject.Name = name;
                                solidObject.Hash = header.ObjectHash;
                                solidObject.MinPoint = header.BoundsMin;
                                solidObject.MaxPoint = header.BoundsMax;

                                solidObject.NumTris = header.NumTris;
                                solidObject.Transform = header.Transform;

                                break;
                            }
                        // 12 40 13 00
                        case 0x00134012:
                            {
                                for (var j = 0; j < chunkSize / 12; j++)
                                {
                                    solidObject.TextureHashes.Add(br.ReadUInt32());

                                    br.BaseStream.Position += 8;
                                }

                                break;
                            }
                        case 0x134900:
                            {
                                var descriptor = BinaryUtil.ReadStruct<SolidObjectDescriptor>(br);

                                Debug.Assert(descriptor.NumTriIdx == 0);

                                solidObject.MeshDescriptor = new SolidMeshDescriptor
                                {
                                    Flags = descriptor.Flags,
                                    HasNormals = true,
                                    NumIndices = descriptor.NumTriIdx,
                                    NumMats = descriptor.MaterialShaderNum,
                                    NumVertexStreams = descriptor.NumVertexStreams,
                                    NumVerts = descriptor.NumVerts
                                };

                                break;
                            }
                        case 0x00134902:
                            {
                                Debug.Assert(chunkSize % 256 == 0);
                                var numMats = chunkSize / 256;

                                for (var j = 0; j < numMats; j++)
                                {
                                    var shadingGroup = BinaryUtil.ReadStruct<Material>(br);
                                    var singleVertexSize = shadingGroup.VertexStream.VertexBytesAndFlag & 0x7FFF;

                                    Debug.Assert(shadingGroup.VertexStream.SizeOfVertexData % singleVertexSize == 0);
                                    Debug.Assert(shadingGroup.TextureNumber[0] < solidObject.TextureHashes.Count);

                                    var numVertices = shadingGroup.VertexStream.SizeOfVertexData / singleVertexSize;
                                    var solidObjectMaterial = new UndercoverMaterial
                                    {
                                        Flags = shadingGroup.MeshFlags,
                                        NumIndices = (uint)shadingGroup.IdxUsed,
                                        Name = $"Unnamed Material #{j + 1:00}",
                                        NumVerts = (uint)numVertices,
                                        VertexStreamIndex = j,
                                        //VertexStreamIndex = shadingGroup.VertexStream.LoadStreamIndex,
                                        TextureHash = solidObject.TextureHashes[shadingGroup.TextureNumber[0]],
                                        EffectId = _effectIdMapping[shadingGroup.MaterialAttribKey],
                                        NumReducedIndices = shadingGroup.NumReducedIdx,
                                        Indices = new ushort[shadingGroup.IdxUsed],
                                        Vertices = new SolidMeshVertex[numVertices]
                                    };

                                    solidObject.Materials.Add(solidObjectMaterial);

                                    //Debug.WriteLine("[{0}] {1} {2}", j, shadingGroup.VertexStream.VertexDataPointer, shadingGroup.IdxUsed);
                                }

                                //Debug.Assert(solidObject.Name != "RD_IN1_INTERSTATECOLLAPSED_101_CHOP_E21_R0_DEINST1_");

                                break;
                            }
                        case 0x134901:
                            {
                                if (chunkSize > 0)
                                {
                                    var vb = new byte[chunkSize];
                                    var readSize = br.Read(vb, 0, vb.Length);
                                    Debug.Assert(readSize == chunkSize);

                                    solidObject.VertexBuffers.Add(vb);
                                }

                                //if (solidObject.Name == "RD_IN1_INTERSTATECOLLAPSED_101_CHOP_E21_R0_DEINST1_")
                                //    Debugger.Break();

                                break;
                            }
                        case 0x134903:
                            {
                                foreach (var material in solidObject.Materials)
                                {
                                    material.Indices = new ushort[material.NumIndices];
                                    for (var j = 0; j < material.Indices.Length; j++)
                                    {
                                        material.Indices[j] = br.ReadUInt16();
                                    }

                                    br.BaseStream.Position += 2 * ((UndercoverMaterial) material).NumReducedIndices;
                                }

                                break;
                            }
                        case 0x00134c02:
                            {
                                if (chunkSize > 0)
                                {
                                    solidObject.Materials[_namedMaterials++].Name =
                                        BinaryUtil.ReadNullTerminatedString(br);
                                }
                                break;
                            }
                        case 0x134015:
                            {
                                var numTextureTypes = br.ReadInt32();
                                var typeOffsets = new uint[numTextureTypes];
                                for (int i = 0; i < numTextureTypes; i++)
                                {
                                    typeOffsets[i] = br.ReadUInt32();
                                }

                                var typeListStart = br.BaseStream.Position;

                                for (int i = 0; i < numTextureTypes; i++)
                                {
                                    br.BaseStream.Position = typeListStart + typeOffsets[i];
                                    solidObject.TextureTypeList.Add(BinaryUtil.ReadNullTerminatedString(br));
                                }
                                break;
                            }
                        default:
                            //Console.WriteLine($"0x{chunkId:X8} [{chunkSize}] @{br.BaseStream.Position}");
                            break;
                    }
                }

                br.BaseStream.Position = chunkEndPos;
            }

            return solidObject;
        }
    }
}
