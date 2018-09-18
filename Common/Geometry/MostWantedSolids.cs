using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Common.Geometry.Data;

namespace Common.Geometry
{
    public class MostWantedSolids : SolidListManager
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

            public byte Blank2, TextureCount, ShaderCount, Blank3;

            public int Blank4;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public float[] BoundsMin;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public float[] BoundsMax;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public float[] Transform;

            public long Blank5;

            public int Unknown2, Unknown3;

            public int Blank6;

            public int Unknown4;

            public float Unknown5, Unknown6;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct SolidObjectShadingGroup
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public float[] BoundsMin;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public float[] BoundsMax;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
            public byte[] TextureIndices;

            public byte ShaderIndex;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 18)]
            public byte[] Blank;

            public int Unknown1;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] Blank2;

            public uint Flags;

            public uint NumVerts;

            public uint NumTris;

            public uint Offset;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
            public byte[] Blank3;

            public uint Length;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] Blank4;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct SolidObjectDescriptor
        {
            public long Blank1;
            public int Unknown1;
            public uint Flags;
            public uint NumMats;
            public uint Blank2;
            public uint NumVertexStreams;
            public long Blank3, Blank4;
            public uint NumIndices;
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

                if (chunkSize == 0) continue;

                if ((chunkId & 0x80000000) == 0x80000000)
                {
                    solidObject = ReadObject(br, chunkSize, solidObject);
                }
                else
                {
                    var padding = 0u;

                    while (br.ReadByte() == 0x11 && br.BaseStream.Position < endPos)
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
                                solidObject.NumShaders = header.ShaderCount;
                                solidObject.NumTextures = header.TextureCount;
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
                                    NumIndices = descriptor.NumIndices,
                                    NumMats = descriptor.NumMats,
                                    NumVertexStreams = descriptor.NumVertexStreams
                                };

                                break;
                            }
                        case 0x00134b02:
                            {
                                var shadingGroupSize = Marshal.SizeOf<SolidObjectShadingGroup>();
                                Debug.Assert(chunkSize % shadingGroupSize == 0);
                                var numMats = chunkSize / shadingGroupSize;

                                var lastUnknown1 = -1;
                                var lastStreamIdx = -1;

                                for (var j = 0; j < numMats; j++)
                                {
                                    var shadingGroup = BinaryUtil.ReadStruct<SolidObjectShadingGroup>(br);

                                    var solidObjectMaterial = new MostWantedMaterial
                                    {
                                        Flags = shadingGroup.Flags,
                                        NumIndices = shadingGroup.Length,
                                        NumTris = shadingGroup.NumTris,
                                        MinPoint = new SimpleVector3(shadingGroup.BoundsMin[0], shadingGroup.BoundsMin[1], shadingGroup.BoundsMin[2]),
                                        MaxPoint = new SimpleVector3(shadingGroup.BoundsMax[0], shadingGroup.BoundsMax[1], shadingGroup.BoundsMax[2]),
                                        Name = $"Unnamed Material #{j + 1:00}",
                                        NumVerts = shadingGroup.NumVerts,
                                        ShaderIndex = shadingGroup.ShaderIndex,
                                        TextureIndices = shadingGroup.TextureIndices,
                                        TriOffset = shadingGroup.Offset
                                    };

                                    uint vsIdx;
                                    if (j == 0)
                                    {
                                        vsIdx = 0;
                                    }
                                    else
                                    {
                                        if (numMats == solidObject.MeshDescriptor.NumVertexStreams)
                                        {
                                            vsIdx = (uint)j;
                                        }
                                        else
                                        {
                                            if (shadingGroup.Unknown1 == lastUnknown1)
                                            {
                                                vsIdx = (uint)lastStreamIdx;
                                            }
                                            else
                                            {
                                                vsIdx = (uint)(lastStreamIdx + 1);
                                            }
                                        }
                                    }

                                    solidObjectMaterial.VertexStreamIndex = (int)vsIdx;

                                    solidObject.Materials.Add(solidObjectMaterial);

                                    solidObject.MeshDescriptor.NumVerts += shadingGroup.NumVerts;

                                    lastUnknown1 = shadingGroup.Unknown1;
                                    lastStreamIdx = (int)vsIdx;
                                }

                                break;
                            }
                        case 0x134b01:
                            {
                                var vb = new VertexBuffer();

                                while (br.BaseStream.Position < chunkEndPos)
                                {
                                    vb.Data.Add(br.ReadSingle());
                                }

                                solidObject.VertexBuffers.Add(vb);

                                break;
                            }
                        case 0x134b03:
                            {
                                for (var j = 0; j < solidObject.NumTris; j++)
                                {
                                    var f1 = (ushort)(br.ReadUInt16() + 1);
                                    var f2 = (ushort)(br.ReadUInt16() + 1);
                                    var f3 = (ushort)(br.ReadUInt16() + 1);

                                    solidObject.Faces.Add(new SolidMeshFace
                                    {
                                        Vtx1 = f1,
                                        Vtx2 = f2,
                                        Vtx3 = f3
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
