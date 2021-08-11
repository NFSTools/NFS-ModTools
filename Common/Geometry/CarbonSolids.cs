using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
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

            public readonly Vector3 BoundsMin;
            public readonly int Blank5;

            public readonly Vector3 BoundsMax;
            public readonly int Blank6;

            public readonly Matrix4x4 Transform;

            public readonly long Blank7;

            public readonly int Unknown2;
            public readonly int Unknown3;

            public readonly int Blank8;

            public readonly int Unknown4;

            public readonly float Unknown5;
            public readonly float Unknown6;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 144)]
        private struct SolidObjectShadingGroup
        {
            public readonly Vector3 BoundsMin;

            public readonly Vector3 BoundsMax;

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
                                            var solidObject = ReadObject(mbr, blocks[0].Length, null);
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
                                            var solidObject = ReadObject(mbr, sorted.Count, null);
                                            solidObject.PostProcessing();

                                            _solidList.Objects.Add(solidObject);
                                        }

                                        sorted.Clear();
                                    }

                                    blocks.Clear();

                                    br.BaseStream.Position = curPos;
                                }
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
                                _namedMaterials = 0;

                                var header = BinaryUtil.ReadStruct<SolidObjectHeader>(br);
                                var name = BinaryUtil.ReadNullTerminatedString(br);

                                solidObject.Name = name;
                                solidObject.Hash = header.Hash;
                                solidObject.MinPoint = header.BoundsMin;
                                solidObject.MaxPoint = header.BoundsMax;
                                solidObject.NumTris = header.NumTris;
                                solidObject.NumShaders = header.ShaderCount;
                                solidObject.NumTextures = header.TextureCount;
                                solidObject.Transform = header.Transform;

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
                                Debug.Assert(chunkSize % 144 == 0);
                                var numMats = chunkSize / 144;

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
                                        MinPoint = shadingGroup.BoundsMin,
                                        MaxPoint = shadingGroup.BoundsMax,
                                        Name = $"Unnamed Material #{j + 1:00}",
                                        NumVerts = shadingGroup.NumVerts,
                                        TextureHash = solidObject.TextureHashes[texIdx],
                                        Unknown1 = (int)shadingGroup.Unknown1
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
                                var vb = new VertexBuffer
                                {
                                    Data = new float[chunkSize >> 2]
                                };

                                var pos = 0;

                                while (br.BaseStream.Position < chunkEndPos)
                                {
                                    var v = br.ReadSingle();

                                    vb.Data[pos++] = v;
                                }

                                solidObject.VertexBuffers.Add(vb);

                                break;
                            }
                        case 0x134b03:
                            {
                                Array.Resize(ref solidObject.Faces, (int)solidObject.Materials.Sum(m => m.NumTris));

                                var faceIndex = 0;

                                foreach (var material in solidObject.Materials)
                                {
                                    for (var j = 0; j < material.NumTris; j++)
                                    {
                                        var f1 = br.ReadUInt16();
                                        var f2 = br.ReadUInt16();
                                        var f3 = br.ReadUInt16();

                                        solidObject.Faces[faceIndex++] = new SolidMeshFace
                                        {
                                            Vtx1 = f1,
                                            Vtx2 = f2,
                                            Vtx3 = f3
                                        };
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
