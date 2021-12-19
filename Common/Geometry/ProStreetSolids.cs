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
    public class ProStreetMaterial : EffectBasedMaterial
    {
    }
    
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

            public byte Version, EndianSwapped;
            public ushort Flags;

            public uint Hash;

            public ushort NumTris;
            public ushort NumVerts;
            public byte NumBones;
            public byte NumTextureTableEntries;
            public byte NumLightMaterials;
            public byte NumPositionMarkerTableEntries;

            public uint Blank2;

            public readonly Vector3 BoundsMin;
            public readonly int Blank5;

            public readonly Vector3 BoundsMax;
            public readonly int Blank6;

            public readonly Matrix4x4 Transform;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public uint[] Unknown3;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            public uint[] Unknown4;
        }

        /// <remarks>
        /// for speedyheart
        /// </remarks>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct TestTrackShadingGroup
        {
            // begin header
            public uint FirstIndex;

            public uint Unknown1;

            public uint Blank;

            public uint Unknown2;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public uint[] Unknown3;

            // end header

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] TextureShaderUsage;

            public uint Unknown4;

            public uint UnknownId;
            public uint Flags;
            public ushort IndicesUsed;
            public ushort Unknown5;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            public uint[] Blank2;

            public uint VertexBufferUsage;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public readonly byte[] Flags2;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
            public uint[] Blank3;

            public uint Unknown6;
            public uint Dummy1;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct SolidObjectShadingGroup
        {
            // begin header
            public uint FirstIndex;

            public uint EffectId;

            public uint Blank;

            public uint Unknown2;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public uint[] Unknown3;

            // end header

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] TextureShaderUsage;

            public uint Unknown4;

            public uint UnknownId;
            public uint Flags;
            public uint IndicesUsed;
            public uint Unknown5;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            public uint[] Blank2;

            public uint VertexBufferUsage; // / 0x20
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public readonly byte[] Flags2;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
            public uint[] Blank3;

            public uint Unknown6;
        }

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

        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 0x18)]
        private struct SolidObjectOffset
        {
            public uint Hash;
            public uint Offset;
            public int LengthCompressed;
            public int Length;
            public uint Flags;
            private uint blank;
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
                    if ((chunkId & 0x80000000) != 0x80000000)
                    {
                        var padding = 0u;

                        while (br.BaseStream.Position < chunkEndPos && br.ReadUInt32() == 0x11111111)
                        {
                            padding += 4;

                            if (br.BaseStream.Position >= chunkEndPos)
                            {
                                break;
                            }
                        }

                        br.BaseStream.Position -= 4;
                        chunkSize -= padding;
                    }

                    switch (chunkId)
                    {
                        case SolidListInfoChunk:
                            {
                                var info = BinaryUtil.ReadStruct<SolidListInfo>(br);

                                _solidList.Filename = info.ClassType;
                                _solidList.PipelinePath = info.PipelinePath;

                                break;
                            }
                        case 0x134004:
                            {
                                while (br.BaseStream.Position < chunkEndPos)
                                {
                                    var och = BinaryUtil.ReadStruct<SolidObjectOffset>(br);
                                    var curPos = br.BaseStream.Position;
                                    br.BaseStream.Position = och.Offset;

                                    if (och.Length == och.LengthCompressed)
                                    {
                                        // Assume that the object is uncompressed.
                                        // If this ever turns out to be false, I don't know what I'll do.
                                        ReadChunks(br, (uint) och.Length);
                                    }
                                    else
                                    {
                                        // Load decompressed object
                                        using (var ms = new MemoryStream())
                                        {
                                            Compression.DecompressCip(br.BaseStream, ms, och.LengthCompressed);

                                            using (var dcr = new BinaryReader(ms))
                                            {
                                                ReadChunks(dcr, (uint) ms.Length);
                                            }
                                        }
                                    }

                                    br.BaseStream.Position = curPos;
                                }

                                return;
                            }
                        case 0x80134010:
                            {
                                var solidObject = ReadObject(br, chunkSize);
                                solidObject.PostProcessing();
                                _solidList.Objects.Add(solidObject);
                                break;
                            }
                    }
                }

                br.BaseStream.Position = chunkEndPos;
            }
        }

        public override void WriteSolidList(ChunkStream chunkStream, SolidList solidList)
        {
            throw new NotImplementedException();
        }

        private SolidObject ReadObject(BinaryReader br, long size, SolidObject solidObject = null)
        {
            if (solidObject == null)
                solidObject = new ProStreetObject();

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

                    while (br.BaseStream.Position < chunkEndPos && br.ReadUInt32() == 0x11111111)
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
                                solidObject.Hash = header.Hash;
                                solidObject.MinPoint = header.BoundsMin;
                                solidObject.MaxPoint = header.BoundsMax;

                                solidObject.Transform = header.Transform;

                                //Debug.Assert(header.Transform == Matrix4x4.Identity);
                                //if (header.Transform != Matrix4x4.Identity)
                                //{
                                //    Debug.WriteLine("Name={0} Transform={1} Flags=0x{2:X4}", name, header.Transform, header.Flags);
                                //}

                                break;
                            }
                        // 12 40 13 00
                        case 0x00134012:
                            {
                                for (var j = 0; j < chunkSize / 8; j++)
                                {
                                    var val = br.ReadUInt32();

                                    if (val != 0)
                                    {
                                        solidObject.TextureHashes.Add(val);
                                    }

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
                                    NumIndices = descriptor.NumTriIndices,
                                    NumMats = descriptor.MaterialShaderCount,
                                    NumVertexStreams = descriptor.NumVertexStreams
                                };

                                break;
                            }
                        case 0x00134b02:
                            {
                                var shadingGroupSize = Marshal.SizeOf<SolidObjectShadingGroup>();
                                Debug.Assert(chunkSize % shadingGroupSize == 0);
                                var numMats = chunkSize / shadingGroupSize;

                                for (var j = 0; j < numMats; j++)
                                {
                                    var shadingGroup = br.GetStruct<SolidObjectShadingGroup>();

                                    solidObject.Materials.Add(new ProStreetMaterial
                                    {
                                        Flags = shadingGroup.Flags,
                                        NumIndices = shadingGroup.IndicesUsed,
                                        NumVerts = shadingGroup.VertexBufferUsage / shadingGroup.Flags2[2],
                                        VertexStreamIndex = j,
                                        Hash = shadingGroup.UnknownId,
                                        TextureHash = solidObject.TextureHashes[shadingGroup.TextureShaderUsage[4]],
                                        EffectId = shadingGroup.EffectId
                                    });

                                    solidObject.MeshDescriptor.NumVerts +=
                                        shadingGroup.VertexBufferUsage / shadingGroup.Flags2[2];
                                }

                                break;
                            }
                        case 0x134b01:
                            {
                                if (chunkSize > 0)
                                {
                                    var vb = new byte[chunkSize];
                                    var readSize = br.Read(vb, 0, vb.Length);
                                    Debug.Assert(readSize == chunkSize);

                                    solidObject.VertexBuffers.Add(vb);
                                }
                                break;
                            }
                        case 0x134b03:
                            {
                                foreach (var material in solidObject.Materials)
                                {
                                    material.Indices = new ushort[material.NumIndices];
                                    for (var j = 0; j < material.NumIndices; j++)
                                    {
                                        material.Indices[j] = br.ReadUInt16();
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
