using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Common.Geometry.Data;

namespace Common.Geometry
{
    public class World15Solids : SolidListManager
    {
        public const float PackedFloatInv = 1 / (float)0x8000;

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

            public long Blank2;

            public uint UnknownOffset;
        }

        // 116 bytes
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct Material
        {
            public uint Flags;
            public uint TextureHash;
            public int EffectID;
            public int Unknown2;

            public Vector3 BoundsMin;

            public Vector3 BoundsMax;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
            public byte[] TextureAssignments;

            public byte LightMaterialNumber;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 18)]
            public byte[] Unknown4;

            public uint NumVerts;
            public uint NumIndices;
            public uint NumTris;
            public uint IndexOffset;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 36)]
            public byte[] Unknown5;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct ObjectHeader
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
            public byte[] InitialData;

            public uint ObjectFlags;

            public uint ObjectHash;

            public uint NumTris;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public readonly byte[] pad2;

            public Vector3 BoundsMin;
            public readonly int Blank5;

            public Vector3 BoundsMax;
            public readonly int Blank6;

            public Matrix4x4 Transform;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            public readonly uint[] Unknown;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public float[] UnknownFloats;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 52)]
        internal struct MeshDescriptor
        {
            public long Unknown1;

            public uint Unknown2;

            public uint Flags;

            public uint MaterialShaderNum;

            public uint Zero;

            public uint NumVertexStreams;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public uint[] Zeroes;

            public uint NumTriangles;

            public uint NumTriIndex;

            public uint Zero2;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct ObjectCompressionHeader
        {
            public readonly uint ObjectHash;
            public readonly uint AbsoluteOffset;
            public readonly uint Size;
            public readonly uint OutSize;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
            private readonly uint[] unknown;
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
                    //var padding = 0u;

                    //while (br.ReadUInt32() == 0x11111111)
                    //{
                    //    padding += 4;
                    //}

                    //br.BaseStream.Position -= 4;
                    //chunkSize -= padding;

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
                        case 0x00134004:
                            {
                                Debug.Assert(chunkSize % Marshal.SizeOf<ObjectCompressionHeader>() == 0, "chunkSize % Marshal.SizeOf<ObjectCompressionHeader>() != 0");

                                while (br.BaseStream.Position < chunkEndPos)
                                {
                                    var och = BinaryUtil.ReadStruct<ObjectCompressionHeader>(br);
                                    var curPos = br.BaseStream.Position;
                                    br.BaseStream.Position = och.AbsoluteOffset;

                                    if (och.Size == och.OutSize)
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

                                        while (bytesRead < och.Size)
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

        /// <inheritdoc />
        /// <summary>
        /// Write a solid list in the NFS:W format.
        /// </summary>
        /// <remarks>Very much WIP.</remarks>
        /// <param name="chunkStream"></param>
        /// <param name="solidList"></param>
        public override void WriteSolidList(ChunkStream chunkStream, SolidList solidList)
        {
            throw new NotImplementedException();
        }

        private World15Object ReadObject(BinaryReader br, long size, bool unpackFloats, World15Object solidObject)
        {
            if (solidObject == null)
                solidObject = new World15Object();

            var endPos = br.BaseStream.Position + size;

            while (br.BaseStream.Position < endPos)
            {
                var chunkId = br.ReadUInt32();
                var chunkSize = br.ReadUInt32();
                var chunkEndPos = br.BaseStream.Position + chunkSize;

                //Console.WriteLine($"@{chunkEndPos}: 0x{chunkId:X8} [{chunkSize}]");

                if ((chunkId & 0x80000000) == 0x80000000)
                {
                    solidObject = ReadObject(br, chunkSize, unpackFloats, solidObject);
                }
                else
                {
                    var padding = 0u;

                    while (br.ReadUInt32() == 0x11111111)
                    {
                        padding += 4;
                    }

                    br.BaseStream.Position -= 4;
                    chunkSize -= padding;

                    switch (chunkId)
                    {
                        case SolidListObjHeadChunk:
                            {
                                _namedMaterials = 0;

                                var header = BinaryUtil.ReadStruct<ObjectHeader>(br);
                                var name = BinaryUtil.ReadNullTerminatedString(br);

                                solidObject.Name = name;
                                solidObject.Hash = header.ObjectHash;
                                solidObject.MinPoint = header.BoundsMin;
                                solidObject.MaxPoint = header.BoundsMax;

                                solidObject.NumTris = header.NumTris;
                                solidObject.NumShaders = 0;
                                solidObject.NumTextures = 0;
                                solidObject.Transform = header.Transform;

                                if (name.StartsWith("TRAF"))
                                {
                                    unpackFloats = true;
                                }

                                break;
                            }
                        case 0x134900:
                            {
                                var descriptor = BinaryUtil.ReadStruct<MeshDescriptor>(br);

                                solidObject.MeshDescriptor = new SolidMeshDescriptor
                                {
                                    Flags = descriptor.Flags,
                                    HasNormals = true,
                                    NumIndices = descriptor.NumTriIndex,
                                    NumMats = descriptor.MaterialShaderNum,
                                    NumVertexStreams = descriptor.NumVertexStreams,
                                    NumTris = descriptor.NumTriangles
                                };

                                break;
                            }
                        case 0x00134b02:
                            {
                                Debug.Assert(chunkSize % solidObject.MeshDescriptor.NumMats == 0);

                                var streamIndex = 0;
                                var lastEffectID = 0;

                                for (var j = 0; j < solidObject.MeshDescriptor.NumMats; j++)
                                {
                                    var shadingGroup = BinaryUtil.ReadStruct<Material>(br);

                                    if (j > 0 && shadingGroup.EffectID != lastEffectID)
                                    {
                                        streamIndex++;
                                    }

                                    var solidObjectMaterial = new World15Material
                                    {
                                        Flags = shadingGroup.Flags,
                                        NumIndices = shadingGroup.NumIndices == 0 ? shadingGroup.NumTris * 3 : shadingGroup.NumIndices,
                                        MinPoint = shadingGroup.BoundsMin,
                                        MaxPoint = shadingGroup.BoundsMax,
                                        Name = $"Unnamed Material #{j + 1:00}",
                                        NumVerts = shadingGroup.NumVerts,
                                        TextureHash = solidObject.TextureHashes[shadingGroup.TextureAssignments[0]],
                                        EffectID = shadingGroup.EffectID,
                                        VertexStreamIndex = streamIndex
                                    };

                                    solidObject.Materials.Add(solidObjectMaterial);

                                    solidObject.MeshDescriptor.NumVerts += shadingGroup.NumVerts;

                                    lastEffectID = shadingGroup.EffectID;
                                }

                                break;
                            }
                        case 0x134012:
                            {
                                for (var j = 0; j < chunkSize / 8; j++)
                                {
                                    solidObject.TextureHashes.Add(br.ReadUInt32());
                                    br.BaseStream.Position += 4;
                                }
                                break;
                            }
                        case 0x134b01:
                            {
                                var vb = new byte[chunkSize];
                                Debug.Assert(br.Read(vb, 0, vb.Length) == chunkSize);

                                solidObject.VertexBuffers.Add(vb);

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
                        case 0x134c06:
                        {
                            Debug.Assert(chunkSize % 0x40 == 0);

                            for (int i = 0; i < chunkSize / 0x40; i++)
                            {
                                solidObject.MorphMatrices.Add(BinaryUtil.ReadStruct<Matrix4x4>(br));
                            }

                            break;
                        }
                        case 0x134c05:
                        {
                            Debug.Assert(chunkSize % 0x10 == 0);
                            var morphList = new List<World15Object.MorphInfo>();

                            for (int i = 0; i < chunkSize / 0x10; i++)
                            {
                                morphList.Add(new World15Object.MorphInfo
                                {
                                    VertexStartIndex = br.ReadInt32(),
                                    VertexEndIndex = br.ReadInt32(),
                                    MorphMatrixIndex = br.ReadInt32()
                                });
                                br.ReadInt32();
                            }

                            solidObject.MorphLists.Add(morphList);

                            break;
                        }
                        default:
                            //Debug.WriteLine($"unhandled chunk in World15Solids: 0x{chunkId:X8} [{chunkSize}] @{br.BaseStream.Position:X}");
                            break;
                    }
                }

                br.BaseStream.Position = chunkEndPos;
            }

            return solidObject;
        }
    }
}
