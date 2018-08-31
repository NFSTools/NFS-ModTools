using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Common;
using GeoEd.Data;

namespace GeoEd.Games
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

        private SolidObject _currentObject;

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

                if ((chunkId & 0x80000000) == 0x80000000)
                {
                    ReadChunks(br, chunkSize);
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
                        case SolidListInfoChunk:
                            {
                                var info = BinaryUtil.ReadStruct<SolidListInfo>(br);

                                _solidList.ClassType = info.ClassType;
                                _solidList.PipelinePath = info.PipelinePath;
                                _solidList.ObjectCount = info.NumObjects;

                                break;
                            }
                        case SolidListObjHeadChunk:
                            {
                                if (_currentObject != null)
                                {
                                    _solidList.Objects.Add(_currentObject);
                                    _namedMaterials = 0;
                                }

                                var header = BinaryUtil.ReadStruct<SolidObjectHeader>(br);
                                var name = BinaryUtil.ReadNullTerminatedString(br);

                                _currentObject = new SolidObject
                                {
                                    Name = name,
                                    Hash = header.Hash,
                                    MinPoint = new SimpleVector4(
                                        header.BoundsMin[0],
                                        header.BoundsMin[1],
                                        header.BoundsMin[2],
                                        header.BoundsMin[3]
                                    ),
                                    MaxPoint = new SimpleVector4(
                                        header.BoundsMax[0],
                                        header.BoundsMax[1],
                                        header.BoundsMax[2],
                                        header.BoundsMax[3]
                                    ),
                                    NumTris = header.NumTris,
                                    NumShaders = header.ShaderCount,
                                    NumTextures = header.TextureCount,
                                    Transform = new SimpleMatrix
                                    {
                                        Data = new float[4, 4]
                                    }
                                };

                                var matrixIdx = 0;

                                for (var j = 0; j < 4; j++)
                                {
                                    for (var k = 0; k < 4; k++)
                                    {
                                        _currentObject.Transform.Data[j, k] = header.Transform[matrixIdx++];
                                    }
                                }

                                break;
                            }
                        case 0x134900:
                            {
                                var descriptor = BinaryUtil.ReadStruct<SolidObjectDescriptor>(br);

                                _currentObject.MeshDescriptor = new SolidMeshDescriptor
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

                                int lastUnknown1 = -1;
                                int lastStreamIdx = -1;

                                for (var j = 0; j < numMats; j++)
                                {
                                    var shadingGroup = BinaryUtil.ReadStruct<SolidObjectShadingGroup>(br);

                                    var solidObjectMaterial = new SolidObjectMaterial
                                    {
                                        Flags = shadingGroup.Flags,
                                        Length = shadingGroup.Length,
                                        NumTris = shadingGroup.NumTris,
                                        MinPoint = new SimpleVector3(shadingGroup.BoundsMin[0], shadingGroup.BoundsMin[1], shadingGroup.BoundsMin[2]),
                                        MaxPoint = new SimpleVector3(shadingGroup.BoundsMax[0], shadingGroup.BoundsMax[1], shadingGroup.BoundsMax[2]),
                                        Name = $"Unnamed Material #{j + 1:00}",
                                        NumVerts = shadingGroup.NumVerts,
                                        ShaderIndex = shadingGroup.ShaderIndex,
                                        TextureIndices = shadingGroup.TextureIndices,
                                        TriOffset = shadingGroup.Offset,
                                        Unknown1 = shadingGroup.Unknown1
                                    };

                                    uint vsIdx;
                                    if (j == 0)
                                    {
                                        vsIdx = 0;
                                    }
                                    else
                                    {
                                        if (numMats == _currentObject.MeshDescriptor.NumVertexStreams)
                                        {
                                            vsIdx = (uint) j;
                                        }
                                        else
                                        {
                                            if (shadingGroup.Unknown1 == lastUnknown1)
                                            {
                                                vsIdx = (uint) lastStreamIdx;
                                            }
                                            else
                                            {
                                                vsIdx = (uint) (lastStreamIdx + 1);
                                            }
                                        }
                                    }

                                    solidObjectMaterial.VertexStreamIndex = (int) vsIdx;

                                    _currentObject.Materials.Add(solidObjectMaterial);

                                    _currentObject.MeshDescriptor.NumVerts += shadingGroup.NumVerts;

                                    lastUnknown1 = shadingGroup.Unknown1;
                                    lastStreamIdx = (int) vsIdx;
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

                                _currentObject.VertexBuffers.Add(vb);

                                break;
                            }
                        case 0x134b03:
                            {
                                for (var j = 0; j < _currentObject.NumTris; j++)
                                {
                                    _currentObject.Faces.Add(new SolidMeshFace
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
                                    _currentObject.Materials[_namedMaterials++].Name =
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
        }
    }
}
