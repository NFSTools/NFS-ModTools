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
            public int EffectId;
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
            public uint IndexOffset; // sequential addition

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
            chunkStream.PaddingAlignment(0x80);

            // 00 40 13 80 - root container
            chunkStream.BeginChunk(0x80134000);

            {
                chunkStream.PaddingAlignment(0x10);

                // 01 40 13 80 - data container
                chunkStream.BeginChunk(0x80134001);

                {
                    // 02 40 13 00 - list info
                    chunkStream.BeginChunk(0x00134002);

                    var info = new SolidListInfo
                    {
                        Marker = 0x1D,
                        NumObjects = solidList.Objects.Count,
                        ClassType = solidList.ClassType,
                        PipelinePath = solidList.PipelinePath,
                        UnknownOffset = 0x80 // ???
                    };

                    chunkStream.WriteStruct(info);
                    chunkStream.Write(new byte[0x1C]);

                    chunkStream.EndChunk();

                    // 03 40 13 00 - hash list
                    chunkStream.BeginChunk(0x00134003);

                    foreach (var solidObject in solidList.Objects.OrderBy(o => o.Hash))
                    {
                        chunkStream.Write(solidObject.Hash);
                        chunkStream.Write(0x00000000);
                    }

                    chunkStream.EndChunk();
                }

                // 08 40 13 80 - empty
                chunkStream.BeginChunk(0x80134008);
                chunkStream.EndChunk();

                chunkStream.EndChunk();

                foreach (var solidObject in solidList.Objects)
                {
                    {
                        // 10 40 13 80 - object container
                        chunkStream.BeginChunk(0x80134010);
                        {
                            // 11 40 13 00 - object info
                            chunkStream.BeginChunk(0x00134011);
                            chunkStream.NextAlignment(0x10, true);

                            var objHeader = new ObjectHeader
                            {
                                BoundsMin = solidObject.MinPoint,
                                BoundsMax = solidObject.MaxPoint,
                                NumTris = solidObject.NumTris,
                                ObjectHash = solidObject.Hash,
                                InitialData = {[12] = 0x16}
                            };

                            chunkStream.WriteStruct(objHeader);

                            var fls = new FixedLenString(solidObject.Name);
                            chunkStream.Write(fls);

                            chunkStream.EndChunk();

                            // 12 40 13 00 - texture list
                            chunkStream.BeginChunk(0x00134012);

                            foreach (var textureHash in solidObject.TextureHashes)
                            {
                                chunkStream.Write(textureHash);
                                chunkStream.Write(0x00000000);
                            }

                            chunkStream.EndChunk();

                            // 00 41 13 80 - mesh data
                            chunkStream.BeginChunk(0x80134100);
                            {
                                // 00 49 13 00 - mesh descriptor
                                chunkStream.BeginChunk(0x00134900);
                                chunkStream.NextAlignment(0x10, true);

                                var md = new MeshDescriptor
                                {
                                    Flags = solidObject.MeshDescriptor.Flags,
                                    MaterialShaderNum = solidObject.MeshDescriptor.NumMats,
                                    NumTriIndex = solidObject.MeshDescriptor.NumIndices,
                                    NumTriangles = solidObject.MeshDescriptor.NumTris,
                                    NumVertexStreams = solidObject.MeshDescriptor.NumVertexStreams,
                                    Unknown1 = 0x12
                                };

                                chunkStream.WriteStruct(md);
                                chunkStream.EndChunk();

                                // 02 4B 13 00 - materials
                                chunkStream.BeginChunk(0x00134B02);
                                chunkStream.NextAlignment(0x10, true);

                                var indexOffset = 0u;

                                foreach (var solidObjectMaterial in solidObject.Materials)
                                {
                                    var material = (World15Material)solidObjectMaterial;
                                    var matStruct = new Material
                                    {
                                        Flags = material.Flags,
                                        EffectId = material.Unknown1,
                                        TextureAssignments = new byte[4],
                                        BoundsMin = material.MinPoint,
                                        BoundsMax = material.MaxPoint
                                    };

                                    matStruct.IndexOffset = indexOffset;
                                    matStruct.NumIndices = material.NumIndices;
                                    matStruct.NumTris = material.NumTris;
                                    matStruct.NumVerts = material.NumVerts;
                                    matStruct.TextureAssignments[0] = material.TextureIndex;
                                    matStruct.TextureAssignments[1] = 0xff;
                                    matStruct.TextureHash = material.TextureHash;

                                    chunkStream.WriteStruct(matStruct);

                                    indexOffset += material.NumIndices;

                                    chunkStream.GetStream().Seek(116 - Marshal.SizeOf<Material>(), SeekOrigin.Current);
                                }

                                chunkStream.EndChunk();

                                // Write faces
                                chunkStream.BeginChunk(0x00134B03);
                                chunkStream.NextAlignment(0x10, true);

                                foreach (var face in solidObject.Faces)
                                {
                                    chunkStream.Write(face.ToArray());
                                }

                                chunkStream.EndChunk();

                                // Write vertex buffers
                                foreach (var vertexBuffer in solidObject.VertexBuffers)
                                {
                                    chunkStream.BeginChunk(0x00134B01);
                                    chunkStream.NextAlignment(0x10, true);
                                    chunkStream.Write(vertexBuffer.Data.ToArray());
                                    chunkStream.EndChunk();
                                }

                                // Write material names
                                foreach (var material in solidObject.Materials)
                                {
                                    chunkStream.BeginChunk(0x00134C02);
                                    chunkStream.Write(new FixedLenString(material.Name));
                                    chunkStream.EndChunk();
                                }
                            }
                            chunkStream.EndChunk();
                        }
                        chunkStream.EndChunk();
                    }
                }
            }

            chunkStream.EndChunk();
        }

        private SolidObject ReadObject(BinaryReader br, long size, bool unpackFloats, SolidObject solidObject)
        {
            if (solidObject == null)
                solidObject = new World15Object();

            solidObject.EnableTransform = false;
            solidObject.IsCompressed = unpackFloats;

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
                                    solidObject.IsCompressed = true;
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

                                var lastUnknown1 = -1;
                                var lastStreamIdx = -1;

                                for (var j = 0; j < solidObject.MeshDescriptor.NumMats; j++)
                                {
                                    var shadingGroup = BinaryUtil.ReadStruct<Material>(br);
                                    byte texIdx = 0;

                                    if (solidObject.TextureHashes.Count > shadingGroup.TextureAssignments[0])
                                    {
                                        texIdx = shadingGroup.TextureAssignments[0];
                                    }

                                    var solidObjectMaterial = new World15Material
                                    {
                                        Flags = shadingGroup.Flags,
                                        NumIndices = shadingGroup.NumIndices == 0 ? shadingGroup.NumTris * 3 : shadingGroup.NumIndices,
                                        NumTris = shadingGroup.NumTris,
                                        MinPoint = shadingGroup.BoundsMin,
                                        MaxPoint = shadingGroup.BoundsMax,
                                        Name = $"Unnamed Material #{j + 1:00}",
                                        NumVerts = shadingGroup.NumVerts,
                                        TextureHash = solidObject.TextureHashes[texIdx],
                                        TextureIndex = texIdx,
                                        Unknown1 = shadingGroup.EffectId
                                    };

                                    int vsIdx;

                                    if (solidObject.MeshDescriptor.NumMats == solidObject.MeshDescriptor.NumVertexStreams)
                                    {
                                        vsIdx = j;
                                    }
                                    else if (j == 0)
                                    {
                                        vsIdx = 0;
                                    }
                                    else if (shadingGroup.EffectId == lastUnknown1)
                                    {
                                        vsIdx = lastStreamIdx;
                                    }
                                    else
                                    {
                                        vsIdx = lastStreamIdx + 1;
                                    }

                                    solidObjectMaterial.VertexStreamIndex = vsIdx;

                                    solidObject.Materials.Add(solidObjectMaterial);

                                    solidObject.MeshDescriptor.NumVerts += shadingGroup.NumVerts;

                                    lastUnknown1 = shadingGroup.EffectId;
                                    lastStreamIdx = vsIdx;
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
                                        //solidObject.Faces.Add(new SolidMeshFace
                                        //{
                                        //    Vtx1 = f1,
                                        //    Vtx2 = f2,
                                        //    Vtx3 = f3
                                        //});
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
