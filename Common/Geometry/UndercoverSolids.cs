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

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public readonly uint[] Zeroes2;

            public readonly uint Unknown3;

            public readonly uint Zero3;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 44)]
        internal struct eVertexStream
        {
            public uint VertexDataPointer;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public uint[] VertexBufferOpaque;

            public uint SizeOfVertexData;
            public byte Stream;
            public byte LoadStreamIndex;
            public short VertexBytesAndFlag;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct Material
        {
            public readonly int FirstIndex; // / 3; @0x0
            public readonly int LastIndex; // / 3; @0x4
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
            public readonly int[] TextureParam;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
            public readonly byte[] TextureNumber; // @0x30

            public readonly byte NumTextures;
            public readonly byte LightMaterialNumber;

            public readonly uint TextureSortKey; // @0x3C
            public readonly uint MeshFlags; // @0x40
            public readonly int IdxUsed; // @0x44

            public eVertexStream VertexStream; // @0x48 [0x2C bytes]

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public readonly int[] Zero4; // @0x74

            public uint MaterialAttribKey; // @0x84
            public uint MaterialAttribPointer; // @0x88

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
            public readonly int[] TextureParamMaterial; // @0x8C

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
            public readonly uint[] TextureNameMaterial; // @0xB4

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 36)]
            public readonly byte[] Zero5; // @0xC0
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
                                for (var j = 0; j < chunkSize / 8; j++)
                                {
                                    var val = br.ReadUInt32();
                                    solidObject.TextureHashes.Add(val);

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
                                    NumIndices = descriptor.NumTriIdx,
                                    NumMats = descriptor.MaterialShaderNum,
                                    NumVertexStreams = descriptor.NumVertexStreams
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

                                    //Debug.Assert(shadingGroup.Zero4[0] == 0);
                                    //Debug.Assert(shadingGroup.Zero4[1] == 0);
                                    //Debug.Assert(shadingGroup.Zero4[2] == 0);
                                    //Debug.Assert(shadingGroup.Zero4[3] == 0);

                                    foreach (var t in shadingGroup.Zero4)
                                        Debug.Assert(t == 0);
                                    foreach (var t in shadingGroup.Zero5)
                                        Debug.Assert(t == 0);

                                    //Debug.WriteLine("object {0} material {1} unknown5={2}", solidObject.Name, j, shadingGroup.LightMaterialNumber);

                                    var singleVertexSize = shadingGroup.VertexStream.VertexBytesAndFlag & 0x7FFF;
                                    Debug.Assert(shadingGroup.VertexStream.SizeOfVertexData % singleVertexSize == 0);
                                    Debug.Assert(shadingGroup.TextureNumber[0] < solidObject.TextureHashes.Count);

                                    //Debug.Assert(shadingGroup.Bytes2[2] == 1);
                                    //Debug.Assert(shadingGroup.LightMaterialNumber == 0xff);

                                    var solidObjectMaterial = new UndercoverMaterial
                                    {
                                        Flags = shadingGroup.MeshFlags,
                                        NumIndices = (uint)shadingGroup.IdxUsed,
                                        NumTris = (uint)(shadingGroup.IdxUsed / 3),
                                        Name = $"Unnamed Material #{j + 1:00}",
                                        NumVerts = (uint)(shadingGroup.VertexStream.SizeOfVertexData / singleVertexSize),
                                        VertexStreamIndex = j,
                                        TextureHash = solidObject.TextureHashes[shadingGroup.TextureNumber[0]]
                                    };

                                    solidObject.Materials.Add(solidObjectMaterial);
                                    solidObject.MeshDescriptor.NumVerts += solidObjectMaterial.NumVerts;
                                }

                                break;
                            }
                        case 0x134901:
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
                        case 0x134903:
                            {
                                Array.Resize(ref solidObject.Faces, (int)solidObject.Materials.Sum(m => m.NumTris));

                                var faceIndex = 0;

                                for (var index = 0; index < solidObject.Materials.Count; index++)
                                {
                                    var material = solidObject.Materials[index];
                                    for (var j = 0; j < material.NumTris; j++)
                                    {
                                        var f1 = br.ReadUInt16();
                                        var f2 = br.ReadUInt16();
                                        var f3 = br.ReadUInt16();

                                        solidObject.Faces[faceIndex++] = new SolidMeshFace
                                        {
                                            Vtx1 = f1,
                                            Vtx2 = f2,
                                            Vtx3 = f3,
                                            MaterialIndex = (byte) index
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
