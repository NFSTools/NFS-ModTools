using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Common.Geometry.Data;

namespace Common.Geometry
{
    public class World15Solids : SolidListManager
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

            public long Blank2;

            public uint UnknownOffset;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct Material
        {
            public uint Flags;
            public uint TextureHash;
            public int Unknown1;
            public int Unknown2;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            public float[] Bounding; // Seems to be (MinX, MinY, MinZ), (MaxX, MaxY, MaxZ)?

            public uint Unknown3;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] MaterialId; // NOTE: first (and second?) byte = ID. Not necessarily in order!

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] Unknown4;

            public uint NumVerts;
            public uint NumIndices;
            public uint NumTris;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct ObjectHeader
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] InitialData;

            public uint ObjectHash;

            public uint NumTris;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public readonly byte[] pad2;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public float[] BoundsMin;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public float[] BoundsMax;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public float[] Transform;

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

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
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
            var stop = false;

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
                        case 0x134004:
                            {
                                Debug.Assert(chunkSize % Marshal.SizeOf<ObjectCompressionHeader>() == 0, "chunkSize % Marshal.SizeOf<ObjectCompressionHeader>() != 0");

                                while (br.BaseStream.Position < chunkEndPos)
                                {
                                    var och = BinaryUtil.ReadStruct<ObjectCompressionHeader>(br);
                                    var curPos = br.BaseStream.Position;

                                    Console.WriteLine("OBJ COMPRESSION");
                                    Console.WriteLine($"\thash   = 0x{och.ObjectHash:X8}");
                                    Console.WriteLine($"\toffset = {och.AbsoluteOffset}");
                                    Console.WriteLine($"\tsize   = {och.Size}");

                                    br.BaseStream.Position = och.AbsoluteOffset;

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
                                        //_blockReader = new BinaryReader(new MemoryStream(blocks[0]));
                                        //ReadUncompressedBlock((uint)blocks[0].Length);

                                        using (var outStream = File.OpenWrite($"object_0x{och.ObjectHash:X8}.bin"))
                                        {
                                            outStream.Write(blocks[0], 0, blocks[0].Length);
                                        }

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

                                        using (var outStream = File.OpenWrite($"object_0x{och.ObjectHash:X8}.bin"))
                                        {
                                            outStream.Write(sorted.ToArray(), 0, sorted.Count);
                                        }

                                        sorted.Clear();
                                    }

                                    blocks.Clear();

                                    br.BaseStream.Position = curPos;
                                }

                                stop = true;

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

                if (stop) break;
            }
        }

        public override void WriteSolidList(ChunkStream chunkStream, SolidList solidList)
        {
            // notes:
            // - model name align by 0x4
            // - null before 01 40 13 80, align 0x10

            // Write root capsule
            chunkStream.BeginChunk(0x80134000);
            chunkStream.PaddingAlignment(0x10);

            {
                // Write info capsule
                chunkStream.BeginChunk(0x80134001);

                {
                    // Write info chunk
                    chunkStream.BeginChunk(0x00134002);

                    var slInfo = new SolidListInfo
                    {
                        PipelinePath = solidList.PipelinePath,
                        ClassType = solidList.ClassType,
                        Marker = 0x1D,
                        NumObjects = solidList.Objects.Count,
                        UnknownOffset = 0x80
                    };

                    chunkStream.WriteStruct(slInfo);
                    chunkStream.Write(new byte[144 - Marshal.SizeOf<SolidListInfo>()]);

                    // End info chunk
                    chunkStream.EndChunk();

                    // Write hashes
                    chunkStream.BeginChunk(0x00134003);

                    var hashes = new List<uint>();

                    foreach (var solidObject in solidList.Objects)
                    {
                        solidObject.Hash = Hasher.BinHash(solidObject.Name);
                        hashes.Add(solidObject.Hash);
                    }

                    hashes.Sort();

                    foreach (var hash in hashes)
                    {
                        chunkStream.Write(hash);
                        chunkStream.Write(0x00000000);
                    }

                    // End hashes
                    chunkStream.EndChunk();

                    // Write empty container
                    chunkStream.BeginChunk(0x80134008);
                    chunkStream.EndChunk();
                }

                // End info capsule
                chunkStream.EndChunk();
            }

            // Write parts

            foreach (var solidObject in solidList.Objects)
            {
                chunkStream.BeginChunk(0x80134010);

                // Write info chunk
                chunkStream.BeginChunk(0x00134011);
                chunkStream.NextAlignment(0x10, true);

                var objectHeader = new ObjectHeader
                {
                    BoundsMin = new[]
                    {
                        solidObject.MinPoint.X,
                        solidObject.MinPoint.Y,
                        solidObject.MinPoint.Z,
                        solidObject.MinPoint.D
                    },
                    BoundsMax = new[]
                    {
                        solidObject.MaxPoint.X,
                        solidObject.MaxPoint.Y,
                        solidObject.MaxPoint.Z,
                        solidObject.MaxPoint.D
                    },
                    Transform = new[]
                    {
                        solidObject.Transform.Data[0, 0],
                        solidObject.Transform.Data[0, 1],
                        solidObject.Transform.Data[0, 2],
                        solidObject.Transform.Data[0, 3],

                        solidObject.Transform.Data[1, 0],
                        solidObject.Transform.Data[1, 1],
                        solidObject.Transform.Data[1, 2],
                        solidObject.Transform.Data[1, 3],

                        solidObject.Transform.Data[2, 0],
                        solidObject.Transform.Data[2, 1],
                        solidObject.Transform.Data[2, 2],
                        solidObject.Transform.Data[2, 3],

                        solidObject.Transform.Data[3, 0],
                        solidObject.Transform.Data[3, 1],
                        solidObject.Transform.Data[3, 2],
                        solidObject.Transform.Data[3, 3]
                    },
                    ObjectHash = solidObject.Hash,
                    NumTris = (uint)solidObject.Faces.Count,
                    InitialData = new byte[16],
                    UnknownFloats = new []
                    {
                        1.0f, // ??? not sure what these do
                        1.0f  // ??? ^
                    }
                };

                objectHeader.InitialData[12] = 0x16;

                chunkStream.WriteStruct(objectHeader);
                chunkStream.Write(new FixedLenString(solidObject.Name));

                chunkStream.EndChunk();

                // Write texture list
                chunkStream.BeginChunk(0x00134012);

                foreach (var textureHash in solidObject.TextureHashes)
                {
                    chunkStream.Write(textureHash);
                    chunkStream.Write(0x00000000);
                }

                // End texture list
                chunkStream.EndChunk();

                // Begin object data
                //chunkStream.BeginChunk(00 41 13 80)
                chunkStream.BeginChunk(0x80134100);

                {
                    // Begin mesh descriptor
                    chunkStream.BeginChunk(0x00134900);

                    Console.WriteLine("MeshDescriptor");
                    chunkStream.NextAlignment(0x10, true);

                    var meshDescriptor = new MeshDescriptor
                    {
                        Flags = solidObject.MeshDescriptor.Flags,
                        MaterialShaderNum = (uint)solidObject.Materials.Count,
                        NumTriangles = (uint)solidObject.Faces.Count,
                        NumTriIndex = (uint)(solidObject.Faces.Count * 3),
                        NumVertexStreams = 1,
                        Unknown2 = 0x12
                    };

                    chunkStream.WriteStruct(meshDescriptor);

                    // End mesh descriptor
                    chunkStream.EndChunk();

                    // Begin materials
                    chunkStream.BeginChunk(0x00134b02);
                    chunkStream.NextAlignment(0x10, true);

                    byte matIdx = 0;
                    foreach (var material in solidObject.Materials)
                    {
                        var matStruct = new Material
                        {
                            Flags = material.Flags,
                            Bounding = new float[6]
                        };

                        matStruct.Bounding[0] = material.MinPoint.X;
                        matStruct.Bounding[1] = material.MinPoint.Y;
                        matStruct.Bounding[2] = material.MinPoint.Z;
                        matStruct.Bounding[3] = material.MaxPoint.X;
                        matStruct.Bounding[4] = material.MaxPoint.Y;
                        matStruct.Bounding[5] = material.MaxPoint.Z;

                        matStruct.MaterialId = new byte[4];
                        matStruct.MaterialId[0] = 0x00;
                        matStruct.MaterialId[1] = 0xff;

                        matStruct.NumIndices = material.NumIndices;
                        matStruct.NumTris = material.NumTris;
                        matStruct.NumVerts = material.NumVerts;
                        matStruct.TextureHash = material.TextureHash;
                        matStruct.Unknown1 = 0;
                        matStruct.Unknown2 = 0;
                        matStruct.Unknown3 = 0;
                        matStruct.Unknown4 = new byte[16];

                        chunkStream.WriteStruct(matStruct);
                        chunkStream.Write(new byte[0x74 - Marshal.SizeOf<Material>()]);

                        matIdx++;
                    }

                    // End materials
                    chunkStream.EndChunk();
                    
                    // Begin faces
                    chunkStream.BeginChunk(0x00134b03);
                    chunkStream.NextAlignment(0x10, true);

                    foreach (var face in solidObject.Faces)
                    {
                        chunkStream.Write(face.Vtx1);
                        chunkStream.Write(face.Vtx2);
                        chunkStream.Write(face.Vtx3);
                    }

                    chunkStream.NextAlignment(0x4);

                    // End faces
                    chunkStream.EndChunk();

                    // Begin vertices
                    chunkStream.BeginChunk(0x00134b01);
                    chunkStream.NextAlignment(0x10, true);

                    // write 36-byte entries
                    foreach (var vertex in solidObject.Vertices)
                    {
                        chunkStream.Write(vertex.X);
                        chunkStream.Write(vertex.Y);
                        chunkStream.Write(vertex.Z);
                        chunkStream.Write(0.001f);
                        chunkStream.Write(0.001f);
                        chunkStream.Write(vertex.U);
                        chunkStream.Write(vertex.V);
                        chunkStream.Write(0.001f);
                        chunkStream.Write(0.001f);
                    }

                    chunkStream.EndChunk();
                    chunkStream.PaddingAlignment(0x10);

                    // Write material names
                    foreach (var material in solidObject.Materials)
                    {
                        chunkStream.BeginChunk(0x00134c02);
                        chunkStream.Write(new FixedLenString(material.Name));
                        chunkStream.EndChunk();
                    }
                }
                // End object data
                chunkStream.EndChunk();

                // End object
                chunkStream.EndChunk();
            }

            // End root capsule
            chunkStream.EndChunk();
        }

        private SolidObject ReadObject(BinaryReader br, long size, bool unpackFloats, SolidObject solidObject)
        {
            if (solidObject == null)
                solidObject = new SolidObject();

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

                                var header = BinaryUtil.ReadStruct<ObjectHeader>(br);
                                var name = BinaryUtil.ReadNullTerminatedString(br);

                                solidObject.Name = name;
                                solidObject.Hash = header.ObjectHash;
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
                                var descriptor = BinaryUtil.ReadStruct<MeshDescriptor>(br);

                                solidObject.MeshDescriptor = new SolidMeshDescriptor
                                {
                                    Flags = descriptor.Flags,
                                    HasNormals = true,
                                    NumIndices = descriptor.NumTriIndex,
                                    NumMats = descriptor.MaterialShaderNum,
                                    NumVertexStreams = descriptor.NumVertexStreams
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

                                    var solidObjectMaterial = new SolidObjectMaterial
                                    {
                                        Flags = shadingGroup.Flags,
                                        NumIndices = shadingGroup.NumIndices == 0 ? shadingGroup.NumTris * 3 : shadingGroup.NumIndices,
                                        NumTris = shadingGroup.NumTris,
                                        MinPoint = new SimpleVector3(shadingGroup.Bounding[0], shadingGroup.Bounding[1], shadingGroup.Bounding[2]),
                                        MaxPoint = new SimpleVector3(shadingGroup.Bounding[3], shadingGroup.Bounding[4], shadingGroup.Bounding[5]),
                                        Name = $"Unnamed Material #{j + 1:00}",
                                        NumVerts = shadingGroup.NumVerts,
                                        TextureHash = shadingGroup.TextureHash
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
                                    else if (shadingGroup.Unknown1 == lastUnknown1)
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

                                    lastUnknown1 = shadingGroup.Unknown1;
                                    lastStreamIdx = vsIdx;

                                    br.BaseStream.Position += 116 - Marshal.SizeOf<Material>();
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
                                var vb = new VertexBuffer();

                                while (br.BaseStream.Position < chunkEndPos)
                                {
                                    var v = br.ReadSingle();

                                    if (unpackFloats)
                                    {
                                        var bytes = BitConverter.GetBytes(v);

                                        unsafe
                                        {
                                            fixed (byte* p = bytes)
                                            {
                                                v = BinaryUtil.GetPackedFloat(p, 0);
                                            }
                                        }
                                    }

                                    vb.Data.Add(v);
                                    //vb.Data.Add(br.ReadSingle());
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
