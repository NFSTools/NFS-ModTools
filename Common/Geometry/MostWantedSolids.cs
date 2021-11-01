using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Common.Geometry.Data;

namespace Common.Geometry
{
    public class MostWantedMaterial : EffectBasedMaterial {}
    
    public class MostWantedSolids : SolidListManager
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

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct SolidObjectShadingGroup
        {
            public readonly Vector3 BoundsMin; // @0x0

            public readonly Vector3 BoundsMax; // @0xC

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
            public readonly byte[] TextureNumber; // @0x18

            public readonly byte LightMaterialNumber; // @0x1D

            public readonly short Unknown; // @0x1E

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x10)]
            public readonly byte[] Blank; // @0x20

            public readonly uint EffectId; // @0x30
            public readonly uint EffectPointer; // @0x34; always null

            public readonly uint Flags; // @0x38
            public readonly uint NumVerts; // @0x3C
            public readonly uint NumTris; // @0x40

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x18)]
            public readonly byte[] Blank2;

            public readonly uint NumIndices;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x8)]
            public readonly byte[] Blank3;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct SolidObjectDescriptor
        {
            public readonly long Blank1;
            public readonly int Unknown1;
            public readonly uint Flags;
            public readonly uint NumMats;
            public readonly uint Blank2;
            public readonly uint NumVertexStreams;
            public readonly long Blank3;
            public readonly long Blank4;
            public readonly uint NumIndices;
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
                    //if ((chunkId & 0x80000000) != 0x80000000)
                    //{
                    //    var padding = 0u;

                    //    while (br.ReadUInt32() == 0x11111111)
                    //    {
                    //        padding += 4;

                    //        if (br.BaseStream.Position >= chunkEndPos)
                    //        {
                    //            break;
                    //        }
                    //    }

                    //    br.BaseStream.Position -= 4;
                    //    chunkSize -= padding;
                    //}

                    switch (chunkId)
                    {
                        case SolidListInfoChunk:
                            {
                                var info = BinaryUtil.ReadStruct<SolidListInfo>(br);

                                _solidList.Filename = info.ClassType;
                                _solidList.PipelinePath = info.PipelinePath;

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
                solidObject = new MostWantedObject();

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
                                var shadingGroupSize = Marshal.SizeOf<SolidObjectShadingGroup>();
                                Debug.Assert(chunkSize % shadingGroupSize == 0);
                                var numMats = chunkSize / shadingGroupSize;

                                var streamIndex = 0;
                                var lastEffectID = 0u;

                                for (var j = 0; j < numMats; j++)
                                {
                                    var shadingGroup = BinaryUtil.ReadStruct<SolidObjectShadingGroup>(br);

                                    if (j > 0 && shadingGroup.EffectId != lastEffectID)
                                    {
                                        streamIndex++;
                                    }

                                    var solidObjectMaterial = new MostWantedMaterial
                                    {
                                        Flags = shadingGroup.Flags,
                                        NumIndices = shadingGroup.NumTris * 3,
                                        MinPoint = shadingGroup.BoundsMin,
                                        MaxPoint = shadingGroup.BoundsMax,
                                        Name = $"Unnamed Material #{j + 1:00}",
                                        NumVerts = shadingGroup.NumVerts,
                                        TextureHash = solidObject.TextureHashes[shadingGroup.TextureNumber[0]],
                                        EffectId = shadingGroup.EffectId,
                                        VertexStreamIndex = streamIndex
                                    };

                                    solidObject.Materials.Add(solidObjectMaterial);

                                    solidObject.MeshDescriptor.NumVerts += shadingGroup.NumVerts;
                                    lastEffectID = shadingGroup.EffectId;
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
