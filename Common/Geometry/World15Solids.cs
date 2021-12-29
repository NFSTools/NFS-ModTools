using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using Common.Geometry.Data;

namespace Common.Geometry
{
    public class World15Material : EffectBasedMaterial
    {
    }
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

        // 116 bytes
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct Material
        {
            public uint Flags;
            public uint TextureHash;
            public uint EffectId;
            public int Unknown2;

            public Vector3 BoundsMin;

            public Vector3 BoundsMax;

            public byte DiffuseMapId;
            public byte NormalMapId;
            public byte HeightMapId;
            public byte SpecularMapId;
            public byte OpacityMapId;

            public byte LightMaterialNumber;

            public ushort Padding;
            public ulong Padding2;
            public ulong Padding3;

            public uint NumVerts;
            public uint NumIndices;
            public uint NumTris;
            public uint IndexOffset;

            public uint Padding4;
            public ulong Padding5;
            public ulong Padding6;
            public ulong Padding7;
            public ulong Padding8;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct ObjectHeader
        {
            public long Padding;

            public uint Padding2;

            public uint ObjectFlags;

            public uint ObjectHash;

            public uint NumTris;
            
            public long Padding3;

            public Vector3 BoundsMin;
            public readonly int Blank5;

            public Vector3 BoundsMax;
            public readonly int Blank6;

            public Matrix4x4 Transform;
            
            public long Padding4, Padding5, Padding6;

            public float Volume, Density;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 52)]
        internal struct MeshDescriptor
        {
            public long Padding;

            public uint Padding2;

            public uint Flags;

            public uint MaterialShaderNum;

            public uint Zero;

            public uint NumVertexStreams;

            public ulong Padding3;
            public uint Padding4;

            public uint NumTriangles;

            public uint NumTriIndex;

            public uint Zero2;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct ObjectCompressionHeader
        {
            public readonly uint Hash;
            public readonly uint Offset;
            public readonly int LengthCompressed;
            public readonly int Length;

            public uint Unknown, Unknown2, Unknown3, Unknown4, Unknown5;
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
                    switch (chunkId)
                    {
                        case SolidListInfoChunk:
                            {
                                var info = BinaryUtil.ReadStruct<SolidListInfo>(br);

                                _solidList.Filename = info.ClassType;
                                _solidList.PipelinePath = info.PipelinePath;

                                break;
                            }
                        case 0x00134004:
                            {
                                Debug.Assert(chunkSize % Marshal.SizeOf<ObjectCompressionHeader>() == 0, "chunkSize % Marshal.SizeOf<ObjectCompressionHeader>() != 0");

                                while (br.BaseStream.Position < chunkEndPos)
                                {
                                    var och = BinaryUtil.ReadUnmanagedStruct<ObjectCompressionHeader>(br);
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

        private World15Object ReadObject(BinaryReader br, long size, World15Object solidObject = null)
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
                    solidObject = ReadObject(br, chunkSize, solidObject);
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

                                var header = BinaryUtil.ReadUnmanagedStruct<ObjectHeader>(br);
                                var name = BinaryUtil.ReadNullTerminatedString(br);

                                solidObject.Name = name;
                                solidObject.Hash = header.ObjectHash;
                                solidObject.MinPoint = header.BoundsMin;
                                solidObject.MaxPoint = header.BoundsMax;

                                solidObject.Transform = header.Transform;

                                break;
                            }
                        case 0x134900:
                            {
                                var descriptor = BinaryUtil.ReadUnmanagedStruct<MeshDescriptor>(br);

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
                                var lastEffectId = 0u;

                                for (var j = 0; j < solidObject.MeshDescriptor.NumMats; j++)
                                {
                                    var shadingGroup = BinaryUtil.ReadUnmanagedStruct<Material>(br);

                                    if (j > 0 && shadingGroup.EffectId != lastEffectId)
                                    {
                                        streamIndex++;
                                    }

                                    var solidObjectMaterial = new World15Material
                                    {
                                        Flags = shadingGroup.Flags,
                                        NumIndices = shadingGroup.NumIndices == 0 ? shadingGroup.NumTris * 3 : shadingGroup.NumIndices,
                                        MinPoint = shadingGroup.BoundsMin,
                                        MaxPoint = shadingGroup.BoundsMax,
                                        NumVerts = shadingGroup.NumVerts,
                                        TextureHash = solidObject.TextureHashes[shadingGroup.DiffuseMapId],
                                        EffectId = shadingGroup.EffectId,
                                        VertexStreamIndex = streamIndex
                                    };

                                    solidObject.Materials.Add(solidObjectMaterial);

                                    solidObject.MeshDescriptor.NumVerts += shadingGroup.NumVerts;

                                    lastEffectId = shadingGroup.EffectId;
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
                                var readSize = br.Read(vb, 0, vb.Length);
                                Debug.Assert(readSize == chunkSize);

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
                                solidObject.MorphMatrices.Add(BinaryUtil.ReadUnmanagedStruct<Matrix4x4>(br));
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
