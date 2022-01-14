using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using Common.Geometry.Data;

namespace Common.Geometry;

public class Underground2SolidReader : SolidReader<Underground2Object, Underground2Material>
{
    public Underground2SolidReader()
    {
        Solid = new Underground2Object();
    }

    protected override void ProcessSolidChunk(BinaryReader binaryReader, uint chunkId, uint chunkSize)
    {
        switch (chunkId)
        {
            case 0x134011:
                ReadSolidHeader(binaryReader);
                break;
            case 0x134012:
                ReadSolidTextures(binaryReader, chunkSize);
                break;
            case 0x134013:
                ReadSolidLightMaterials(binaryReader, chunkSize);
                break;
            case 0x134017:
                ReadSolidPlatNormalSmoother(binaryReader);
                break;
            case 0x134018:
                ReadSolidPlatSmoothVertices(binaryReader, chunkSize);
                break;
            case 0x134019:
                ReadSmoothVerticesPlat(binaryReader, chunkSize);
                break;
            case 0x13401A:
                ReadSolidPositionMarkers(binaryReader, chunkSize);
                break;
            default:
                throw new InvalidDataException(
                    $"Not sure how to handle solid chunk: 0x{chunkId:X} at 0x{binaryReader.BaseStream.Position:X}");
        }
    }

    private void ReadSolidHeader(BinaryReader binaryReader)
    {
        BinaryUtil.AlignReader(binaryReader, 0x10);
        var header = BinaryUtil.ReadStruct<SolidObjectHeader>(binaryReader);
        Debug.Assert(header.Version == 0x16, "header.Version == 0x16");

        var name = new string(binaryReader.ReadChars(0x1C)).Trim('\0');

        Solid.Name = name;
        Solid.Hash = header.Hash;
        Solid.MinPoint = header.BoundsMin;
        Solid.MaxPoint = header.BoundsMax;

        Solid.Transform = header.Transform;
    }

    protected override void ProcessPlatChunk(BinaryReader binaryReader, uint chunkId, uint chunkSize)
    {
        switch (chunkId)
        {
            case 0x134900:
                ReadSolidPlatInfo(binaryReader);
                break;
            case 0x134b01:
                ReadSolidPlatVertexBuffer(binaryReader, chunkSize);
                break;
            case 0x134b02:
                ReadSolidPlatMeshEntries(binaryReader, chunkSize);
                break;
            case 0x134b03:
                ReadSolidPlatIndices(binaryReader);
                break;
            default:
                throw new InvalidDataException(
                    $"Not sure how to handle mesh chunk: 0x{chunkId:X} at 0x{binaryReader.BaseStream.Position:X}");
        }
    }

    private void ReadSmoothVerticesPlat(BinaryReader binaryReader, uint chunkSize)
    {
        Debug.Assert(chunkSize % 12 == 0, "chunkSize % 12 == 0");
    }

    private void ReadSolidPlatSmoothVertices(BinaryReader binaryReader, uint chunkSize)
    {
        Debug.Assert(chunkSize % 8 == 0, "chunkSize % 8 == 0");
    }

    private void ReadSolidPlatNormalSmoother(BinaryReader binaryReader)
    {
        binaryReader.BaseStream.Position += 12;
    }

    private void ReadSolidPlatIndices(BinaryReader binaryReader)
    {
        BinaryUtil.AlignReader(binaryReader, 0x10);
        foreach (var material in Solid.Materials)
        {
            material.Indices = new ushort[material.NumIndices];
            for (var j = 0; j < material.NumIndices; j++) material.Indices[j] = binaryReader.ReadUInt16();
        }
    }

    private void ReadSolidPlatMeshEntries(BinaryReader binaryReader, uint chunkSize)
    {
        chunkSize -= BinaryUtil.AlignReader(binaryReader, 0x10);
        Debug.Assert(chunkSize % 60 == 0);
        var numMats = chunkSize / 60;

        for (var j = 0; j < numMats; j++)
        {
            var shadingGroup = BinaryUtil.ReadStruct<SolidObjectShadingGroup>(binaryReader);

            Solid.Materials.Add(new Underground2Material
            {
                Flags = shadingGroup.Flags,
                MinPoint = shadingGroup.BoundsMin,
                MaxPoint = shadingGroup.BoundsMax,
                NumIndices = shadingGroup.Length,
                VertexSetIndex = 0,
                TextureHash = Solid.TextureHashes[(int)shadingGroup.TextureIndex]
            });
        }
    }

    private void ReadSolidPlatVertexBuffer(BinaryReader binaryReader, uint chunkSize)
    {
        chunkSize -= BinaryUtil.AlignReader(binaryReader, 0x80);
        var vb = new byte[chunkSize];

        if (binaryReader.Read(vb, 0, vb.Length) != vb.Length)
            throw new InvalidDataException($"Failed to read {vb.Length} bytes of vertex data");

        VertexBuffers.Add(vb);
    }

    private void ReadSolidPlatInfo(BinaryReader binaryReader)
    {
        BinaryUtil.AlignReader(binaryReader, 0x10);
        var descriptor = BinaryUtil.ReadUnmanagedStruct<SolidObjectDescriptor>(binaryReader);
        NumVertices = descriptor.NumVerts;
    }

    protected override SolidMeshVertex GetVertex(BinaryReader reader, Underground2Material material, int stride)
    {
        return stride switch
        {
            60 => new SolidMeshVertex
            {
                Position = BinaryUtil.ReadVector3(reader),
                Normal = BinaryUtil.ReadVector3(reader),
                Color = reader.ReadUInt32(),
                TexCoords = BinaryUtil.ReadVector2(reader),
                BlendWeight = BinaryUtil.ReadVector3(reader),
                BlendIndices = BinaryUtil.ReadVector3(reader)
            },
            36 => new SolidMeshVertex
            {
                Position = BinaryUtil.ReadVector3(reader),
                Normal = BinaryUtil.ReadVector3(reader),
                Color = reader.ReadUInt32(),
                TexCoords = BinaryUtil.ReadVector2(reader)
            },
            24 => new SolidMeshVertex
            {
                Position = BinaryUtil.ReadVector3(reader),
                Color = reader.ReadUInt32(),
                TexCoords = BinaryUtil.ReadVector2(reader)
            },
            _ => throw new Exception($"Cannot handle vertex size: {stride}")
        };
    }

    protected override void ProcessVertices(ref SolidMeshVertex[] vertices, int vertexStreamId)
    {
        //
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct SolidObjectHeader
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public readonly byte[] Blank;

        public readonly byte Version;
        [MarshalAs(UnmanagedType.I1)] public readonly bool EndianSwapped;
        public readonly ushort Flags;

        public readonly uint Hash;

        public readonly ushort NumPolys;
        public readonly ushort NumVerts;

        public readonly byte NumBones;
        public readonly byte NumTextureTableEntries;
        public readonly byte NumLightMaterials;
        public readonly byte NumPositionMarkerTableEntries;

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
        public readonly float Unknown4;
        public readonly float Unknown5;
        public readonly int Unknown6;
        public readonly int Unknown7;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct SolidObjectDescriptor
    {
        public readonly long Blank1;
        public readonly int Version;
        public readonly uint Flags;
        public readonly uint NumMats;
        public readonly uint Blank2;
        public readonly uint Blank3;
        public readonly uint Blank4;
        public readonly uint Blank5;
        public readonly uint NumTris;
        public readonly uint Blank6;
        public readonly uint Blank7;
        public readonly uint Blank8;
        public readonly uint NumVerts;
        public readonly uint Blank9;
        public readonly uint Blank10;
        public readonly uint Blank11;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct SolidObjectShadingGroup
    {
        public readonly Vector3 BoundsMin;

        public readonly uint Length; // indices

        public readonly Vector3 BoundsMax;

        public readonly uint TextureIndex;
        public readonly uint ShaderIndex;
        public readonly uint D1, D2, D3, D4;
        public readonly uint Offset; // indices
        public readonly uint Flags;
    }
}