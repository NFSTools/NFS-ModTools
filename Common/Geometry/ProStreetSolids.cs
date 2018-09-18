using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Common.Geometry.Data;

namespace Common.Geometry
{
    public class ProStreetSolids : SolidListManager
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct SolidListInfo
        {
            public long Blank;

            public int Marker; // this doesn't change between games for some reason... rather unfortunate

            public int NumObjects;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x38)]
            public string PipelinePath;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
            public string ClassType;

            public int UnknownOffset, UnknownSize;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct SolidObjectHeader
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
            public byte[] Blank;

            public uint Unknown1;

            public uint Hash;

            public uint NumTris;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] Unknown2;

            public uint Blank2;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public float[] BoundsMin;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public float[] BoundsMax;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public float[] Transform;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public uint[] Unknown3;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            public uint[] Unknown4;
        }

        //[StructLayout(LayoutKind.Sequential, Pack = 1)]
        //private struct SolidObjectShadingGroup
        //{
        //    // begin header
        //    public uint FirstIndex;

        //    public uint Unknown1;

        //    public uint Blank;

        //    public uint Unknown2;

        //    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        //    public uint[] Unknown3;

        //    // end header

        //    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        //    public byte[] TextureShaderUsage;

        //    public uint Unknown4;

        //    public uint UnknownId;
        //    public uint Flags;
        //    public uint IndicesUsed;
        //    public uint Flags2;
        //}

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct SolidObjectDescriptor
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public uint[] Blank1;

            public uint Unknown1;

            public uint Flags;

            public uint MaterialShaderCount;

            public uint Blank2;

            public uint NumVertexStreams; // should be 0, real count == MaterialShaderCount

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public uint[] Blank3;

            public uint NumTris; // 0 if NumTriIndices

            public uint NumTriIndices; // 0 if NumTris

            public uint Unknown2;

            public uint Blank4;
        }

        private const uint SolidListInfoChunk = 0x134002;
        private const uint SolidListObjHeadChunk = 0x134011;

        private SolidList _solidList;

        private int _namedMaterials;

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

                if ((chunkId & 0x80000000) == 0x80000000 && chunkId != 0x80134010)
                {
                    ReadChunks(br, chunkSize);
                }
                else
                {
                    if ((chunkId & 0x80000000) != 0x80000000)
                    {
                        var padding = 0u;

                        while (br.ReadByte() == 0x11)
                        {
                            padding++;
                        }

                        br.BaseStream.Position--;

                        if (padding % 2 != 0) padding--;

                        chunkSize -= padding;
                    }

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
                        case 0x80134010:
                            {
                                var solidObject = ReadObject(br, chunkSize, null);
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

        private SolidObject ReadObject(BinaryReader br, long size, SolidObject solidObject)
        {
            if (solidObject == null)
                solidObject = new SolidObject();

            var endPos = br.BaseStream.Position + size;

            while (br.BaseStream.Position < endPos)
            {
                var chunkId = br.ReadUInt32();
                var chunkSize = br.ReadUInt32();
                var chunkEndPos = br.BaseStream.Position + chunkSize;

                if ((chunkId & 0x80000000) == 0x80000000)
                {
                    solidObject = ReadObject(br, chunkSize, solidObject);
                }
                else
                {
                    var padding = 0u;

                    while (br.ReadByte() == 0x11)
                    {
                        padding++;
                    }

                    br.BaseStream.Position--;

                    if (padding % 2 != 0) padding--;

                    chunkSize -= padding;

                    switch (chunkId)
                    {
                        case SolidListObjHeadChunk:
                            {
                                _namedMaterials = 0;

                                var header = BinaryUtil.ReadStruct<SolidObjectHeader>(br);
                                var name = BinaryUtil.ReadNullTerminatedString(br);

                                solidObject.Name = name;
                                solidObject.Hash = header.Hash;
                                solidObject.MinPoint = new SimpleVector4(
                                    header.BoundsMin[0],
                                    header.BoundsMin[1],
                                    header.BoundsMin[2],
                                    header.BoundsMin[3]
                                );

                                solidObject.MaxPoint = new SimpleVector4(
                                    header.BoundsMax[0],
                                    header.BoundsMax[1],
                                    header.BoundsMax[2],
                                    header.BoundsMax[3]
                                );

                                solidObject.NumTris = header.NumTris;
                                solidObject.NumShaders = 0;
                                solidObject.NumTextures = 0;
                                solidObject.Transform = new SimpleMatrix
                                {
                                    Data = new[,]
                                    {
                                        { header.Transform[0], header.Transform[1], header.Transform[2], header.Transform[3]},
                                        { header.Transform[4], header.Transform[5], header.Transform[6], header.Transform[7]},
                                        { header.Transform[8], header.Transform[9], header.Transform[10], header.Transform[11]},
                                        { header.Transform[12], header.Transform[13], header.Transform[14], header.Transform[15]}
                                    }
                                };

                                break;
                            }
                        case 0x134900:
                            {
                                var descriptor = BinaryUtil.ReadStruct<SolidObjectDescriptor>(br);

                                solidObject.MeshDescriptor = new SolidMeshDescriptor
                                {
                                    Flags = descriptor.Flags,
                                    HasNormals = true,
                                    NumIndices = descriptor.NumTriIndices,
                                    NumMats = descriptor.MaterialShaderCount,
                                    NumVertexStreams = descriptor.NumVertexStreams
                                };

                                break;
                            }
                        case 0x00134b02:
                            {
                                for (var j = 0; j < chunkSize / 128; j++)
                                {
                                    var pos = br.BaseStream.Position;

                                    var firstIndex = br.ReadUInt32();
                                    var unknown1 = br.ReadUInt32();
                                    br.ReadUInt32();
                                    var unknown2 = br.ReadUInt32();
                                    br.ReadUInt32();
                                    br.ReadUInt32();
                                    var textureUsage = br.ReadBytes(8);
                                    var unknown3 = br.ReadUInt32();
                                    var unknownId = br.ReadUInt32();
                                    var flags = br.ReadUInt32();
                                    var indicesUsed = br.ReadUInt32();

                                    br.ReadUInt32();

                                    br.BaseStream.Position += 24; // skip 6 blanks

                                    var vertBufUsage = br.ReadUInt32();
                                    var vertsUsed = vertBufUsage >> 5;
                                    br.ReadUInt32();
                                    br.BaseStream.Position += 40; // skip 10 blanks
                                    br.ReadUInt32();

                                    var solidObjectMaterial = new MostWantedMaterial
                                    {
                                        Flags = flags,
                                        NumIndices = indicesUsed,
                                        NumTris = indicesUsed / 3,
                                        Name = $"Unnamed Material #{j + 1:00}",
                                        NumVerts = vertsUsed,
                                        ShaderIndex = textureUsage[5],
                                        TextureIndices = new[]
                                        {
                                            textureUsage[0],
                                            textureUsage[1],
                                            textureUsage[2],
                                            textureUsage[3],
                                            textureUsage[4]
                                        },
                                        VertexStreamIndex = j,
                                        Hash = unknownId
                                    };

                                    solidObject.Materials.Add(solidObjectMaterial);

                                    solidObject.MeshDescriptor.NumVerts += vertsUsed;

                                    br.BaseStream.Position = pos + 128;
                                }

                                //var shadingGroupSize = Marshal.SizeOf<SolidObjectShadingGroup>();
                                //Debug.Assert(chunkSize % shadingGroupSize == 0);
                                //var numMats = chunkSize / shadingGroupSize;

                                //for (var j = 0; j < numMats; j++)
                                //{
                                //    var shadingGroup = BinaryUtil.ReadStruct<SolidObjectShadingGroup>(br);
                                //    //var shadingGroup = BinaryUtil.ReadStruct<SolidObjectShadingGroup>(br);
                                //    shadingGroup.VertsUsed >>= 5;
                                //    var solidObjectMaterial = new MostWantedMaterial
                                //    {
                                //        Flags = shadingGroup.Flags,
                                //        NumIndices = shadingGroup.IndicesUsed,
                                //        NumTris = shadingGroup.IndicesUsed / 3,
                                //        Name = $"Unnamed Material #{j + 1:00}",
                                //        NumVerts = shadingGroup.VertsUsed,
                                //        ShaderIndex = shadingGroup.TextureShaderUsage[5],
                                //        TextureIndices = new[]
                                //        {
                                //            shadingGroup.TextureShaderUsage[0],
                                //            shadingGroup.TextureShaderUsage[1],
                                //            shadingGroup.TextureShaderUsage[2],
                                //            shadingGroup.TextureShaderUsage[3],
                                //            shadingGroup.TextureShaderUsage[4]
                                //        },
                                //        VertexStreamIndex = j,
                                //        Hash = shadingGroup.UnknownId
                                //    };


                                //    solidObject.Materials.Add(solidObjectMaterial);

                                //    solidObject.MeshDescriptor.NumVerts += shadingGroup.VertsUsed;

                                //    //lastUnknown1 = shadingGroup.Unknown1;
                                //    //lastStreamIdx = (int)vsIdx;
                                //}

                                break;
                            }
                        case 0x134b01:
                            {
                                var vb = new VertexBuffer();

                                while (br.BaseStream.Position < chunkEndPos)
                                {
                                    var v = br.ReadSingle();

                                    var bytes = BitConverter.GetBytes(v);
                                    Array.Reverse(bytes);

                                    float nv;

                                    unsafe
                                    {
                                        fixed (byte* bp = bytes)
                                        {
                                            nv = BinaryUtil.GetPackedFloat(bp, 0);
                                        }
                                    }

                                    vb.Data.Add(nv);
                                    //vb.Data.Add(br.ReadSingle());
                                }

                                solidObject.VertexBuffers.Add(vb);

                                break;
                            }
                        case 0x134b03:
                            {
                                for (var j = 0; j < solidObject.NumTris; j++)
                                {
                                    solidObject.Faces.Add(new SolidMeshFace
                                    {
                                        Vtx1 = br.ReadUInt16(),
                                        Vtx2 = br.ReadUInt16(),
                                        Vtx3 = br.ReadUInt16(),
                                    });
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
