using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Common.Geometry.Data;

namespace Common.Geometry
{
    public class CarbonSolids : SolidListManager
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
        private struct SolidObjectHeader
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
            public readonly byte[] Blank;

            public readonly uint Unknown1;

            public readonly uint Hash;

            public readonly uint NumTris;

            public readonly byte Blank2;
            public readonly byte TextureCount;
            public readonly byte ShaderCount;
            public readonly byte Blank3;

            public readonly int Blank4;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public readonly float[] BoundsMin;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public readonly float[] BoundsMax;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public readonly float[] Transform;

            public readonly long Blank5;

            public readonly int Unknown2;
            public readonly int Unknown3;

            public readonly int Blank6;

            public readonly int Unknown4;

            public readonly float Unknown5;
            public readonly float Unknown6;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct SolidObjectShadingGroup
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public readonly float[] BoundsMin;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public readonly float[] BoundsMax;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public readonly byte[] TextureIndices;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            private readonly byte[] Blank;

            public readonly uint Unknown1;

            public readonly uint Unknown2;

            public readonly uint Unknown3;

            public readonly uint UnknownId;

            public readonly uint NumVerts;
            public readonly uint Flags;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            private readonly uint[] Blank2;

            public readonly uint NumTris;
            public readonly uint BaseIdx;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
            private readonly uint[] Blank3;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct SolidObjectDescriptor
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            private readonly uint[] Blank;

            private readonly uint Unknown1;

            public readonly uint Flags;

            public readonly uint NumMats;

            private readonly uint Blank2;

            public readonly uint NumVertexStreams;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            private readonly uint[] Blank3;

            public readonly uint NumIndices; // 0 if NumTris
            public readonly uint NumTris; // 0 if NumIndices
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

                        if (padding % 2 != 0)
                        {
                            padding--;
                        }

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
                solidObject = new CarbonObject();

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

                    while (br.ReadByte() == 0x11)
                    {
                        padding++;
                    }

                    br.BaseStream.Position--;

                    if (padding % 2 != 0)
                    {
                        padding--;
                        br.BaseStream.Position--;
                    }

                    chunkSize -= padding;

                    switch (chunkId)
                    {
                        case SolidListObjHeadChunk:
                            {
                                var solidHeaderSize = Marshal.SizeOf<SolidObjectHeader>();
                                Debug.Assert(solidHeaderSize <= chunkSize);

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
                        // 12 40 13 00
                        case 0x00134012:
                        {
                            for (var j = 0; j < chunkSize / 8; j++)
                            {
                                solidObject.TextureHashes.Add(br.ReadUInt32());
                                br.BaseStream.Position += 4;
                            }

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
                                    var texIdx = 0;

                                    if (solidObject.TextureHashes.Count > shadingGroup.TextureIndices[0])
                                    {
                                        texIdx = shadingGroup.TextureIndices[0];
                                    }

                                    var solidObjectMaterial = new CarbonMaterial
                                    {
                                        Flags = shadingGroup.Flags,
                                        NumIndices = shadingGroup.NumTris * 3,
                                        NumTris = shadingGroup.NumTris,
                                        MinPoint = new SimpleVector3(shadingGroup.BoundsMin[0], shadingGroup.BoundsMin[1], shadingGroup.BoundsMin[2]),
                                        MaxPoint = new SimpleVector3(shadingGroup.BoundsMax[0], shadingGroup.BoundsMax[1], shadingGroup.BoundsMax[2]),
                                        Name = $"Unnamed Material #{j + 1:00}",
                                        NumVerts = shadingGroup.NumVerts,
                                        TextureHash = solidObject.TextureHashes[texIdx],
                                        Unknown1 = shadingGroup.Unknown1
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

                                    lastUnknown1 = (int)shadingGroup.Unknown1;
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
                                foreach (var material in solidObject.Materials)
                                {
                                    for (var j = 0; j < material.NumTris; j++)
                                    {
                                        var f1 = br.ReadUInt16();
                                        var f2 = br.ReadUInt16();
                                        var f3 = br.ReadUInt16();

                                        solidObject.Faces.Add(new SolidMeshFace
                                        {
                                            Vtx1 = f1,
                                            Vtx2 = f2,
                                            Vtx3 = f3
                                        });
                                    }
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
