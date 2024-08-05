using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using Common.Geometry.Data;

namespace Common.Geometry;

public class WorldSolidReader : SolidReader<World15Object, World15Material>
{
    private int _namedMaterials;

    public WorldSolidReader()
    {
        Solid = new World15Object();
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
            // case 0x134015:
            //     ReadSolidTextureTypes(binaryReader, chunkSize);
            //     break;
            // case 0x134017:
            //     ReadSolidNormalSmoother(binaryReader);
            //     break;
            // case 0x134018:
            //     ReadSolidSmoothVertices(binaryReader, chunkSize);
            //     break;
            // case 0x134019:
            //     ReadSolidSmoothVerticesPlat(binaryReader, chunkSize);
            //     break;
            case 0x13401A:
                ReadSolidPositionMarkers(binaryReader, chunkSize);
                break;
            case 0x13401D:
                ReadSolidMorphTargets(binaryReader, chunkSize);
                break;
            // case 0x13401E:
            //     ReadSolidMorphChildren(binaryReader, chunkSize);
            //     break;
            default:
                throw new InvalidDataException(
                    $"Not sure how to handle solid chunk: 0x{chunkId:X} at 0x{binaryReader.BaseStream.Position:X}");
        }
    }

    private void ReadSolidMorphTargets(BinaryReader binaryReader, uint chunkSize)
    {
        Debug.Assert(chunkSize % 12 == 0, "chunkSize % 12 == 0");
        // TODO: Do something with this data
        binaryReader.BaseStream.Position += chunkSize;
    }

    private void ReadSolidHeader(BinaryReader binaryReader)
    {
        BinaryUtil.AlignReader(binaryReader, 0x10);

        var header = BinaryUtil.ReadStruct<SolidObjectHeader>(binaryReader);
        Debug.Assert(header.Version == 0x16, "header.Version == 0x16");

        var name = BinaryUtil.ReadNullTerminatedString(binaryReader);

        Solid.Name = name;
        Solid.Hash = header.Hash;
        Solid.MinPoint = header.BoundsMin;
        Solid.MaxPoint = header.BoundsMax;
        Solid.PivotMatrix = header.Transform;
    }

    protected override void ProcessPlatChunk(BinaryReader binaryReader, uint chunkId, uint chunkSize)
    {
        switch (chunkId)
        {
            case 0x134900:
                ReadSolidPlatInfo(binaryReader);
                break;
            case 0x134B01:
                ReadSolidPlatVertexBuffer(binaryReader, chunkSize);
                break;
            case 0x134B02:
                ReadSolidPlatMeshEntries(binaryReader, chunkSize);
                break;
            case 0x134B03:
                ReadSolidPlatIndices(binaryReader);
                break;
            // case 0x134F01:
            //     ReadSolidPlatMaterialNameHashes(binaryReader);
            //     break;
            case 0x134C02:
                ReadSolidPlatMeshEntryName(binaryReader, chunkSize);
                break;
            case 0x134c05:
                ReadSolidPlatTransformationList(binaryReader, chunkSize);
                break;
            case 0x134c06:
                ReadSolidPlatTransformationMatrices(binaryReader, chunkSize);
                break;
            // case 0x134C04:
            //     ReadUCapFrameWeights(binaryReader, chunkSize);
            //     break;
            default:
                throw new InvalidDataException(
                    $"Not sure how to handle mesh chunk: 0x{chunkId:X} at 0x{binaryReader.BaseStream.Position:X}");
        }
    }

    private void ReadSolidPlatTransformationList(BinaryReader binaryReader, uint chunkSize)
    {
        chunkSize -= BinaryUtil.AlignReader(binaryReader, 0x10);
        Debug.Assert(chunkSize % 0x10 == 0);
        var morphList = new List<World15Object.MorphInfo>();

        for (var i = 0; i < chunkSize / 0x10; i++)
        {
            morphList.Add(new World15Object.MorphInfo
            {
                VertexStartIndex = binaryReader.ReadInt32(),
                VertexEndIndex = binaryReader.ReadInt32(),
                MorphMatrixIndex = binaryReader.ReadInt32()
            });
            binaryReader.ReadInt32();
        }

        Solid.MorphLists.Add(morphList);
    }

    private void ReadSolidPlatTransformationMatrices(BinaryReader binaryReader, uint chunkSize)
    {
        chunkSize -= BinaryUtil.AlignReader(binaryReader, 0x10);
        Debug.Assert(chunkSize % 0x40 == 0);

        for (var i = 0; i < chunkSize / 0x40; i++)
            Solid.MorphMatrices.Add(BinaryUtil.ReadUnmanagedStruct<Matrix4x4>(binaryReader));
    }

    private void ReadSolidPlatMeshEntryName(BinaryReader binaryReader, uint chunkSize)
    {
        if (chunkSize > 0)
            Solid.Materials[_namedMaterials++].Name =
                BinaryUtil.ReadNullTerminatedString(binaryReader);
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
        Debug.Assert(chunkSize % 116 == 0, "chunkSize % 116 == 0");

        var streamIndex = 0;
        var lastEffectId = 0u;

        for (var j = 0; j < chunkSize / 116; j++)
        {
            var shadingGroup = BinaryUtil.ReadUnmanagedStruct<Material>(binaryReader);

            if (j > 0 && shadingGroup.EffectId != lastEffectId) streamIndex++;

            Solid.Materials.Add(new World15Material
            {
                Flags = shadingGroup.Flags,
                NumIndices = shadingGroup.NumIndices == 0 ? shadingGroup.NumTris * 3 : shadingGroup.NumIndices,
                MinPoint = shadingGroup.BoundsMin,
                MaxPoint = shadingGroup.BoundsMax,
                NumVerts = shadingGroup.NumVerts,
                DiffuseTextureHash = Solid.TextureHashes[shadingGroup.DiffuseMapId],
                NormalTextureHash = shadingGroup.NormalMapId == shadingGroup.DiffuseMapId
                    ? null
                    : Solid.TextureHashes[shadingGroup.NormalMapId],
                SpecularTextureHash = shadingGroup.SpecularMapId == shadingGroup.DiffuseMapId
                    ? null
                    : Solid.TextureHashes[shadingGroup.SpecularMapId],
                EffectId = shadingGroup.EffectId,
                VertexSetIndex = streamIndex,
                SortKey = shadingGroup.SortKey
            });

            NumVertices += shadingGroup.NumVerts;
            lastEffectId = shadingGroup.EffectId;
        }
    }

    private void ReadSolidPlatVertexBuffer(BinaryReader binaryReader, uint chunkSize)
    {
        chunkSize -= BinaryUtil.AlignReader(binaryReader, 0x10);
        var vb = new byte[chunkSize];

        if (binaryReader.Read(vb, 0, vb.Length) != vb.Length)
            throw new InvalidDataException($"Failed to read {vb.Length} bytes of vertex data");

        VertexBuffers.Add(vb);
    }

    private void ReadSolidPlatInfo(BinaryReader binaryReader)
    {
        BinaryUtil.AlignReader(binaryReader, 0x10);
        BinaryUtil.ReadUnmanagedStruct<SolidObjectDescriptor>(binaryReader);
    }

    protected override SolidMeshVertex GetVertex(BinaryReader reader, World15Material material, int stride)
    {
        var vertex = new SolidMeshVertex();

        var id = (InternalEffectId)material.EffectId;

        switch (id)
        {
            case InternalEffectId.WorldShader:
            case InternalEffectId.GLASS_REFLECT:
            case InternalEffectId.WorldZBiasShader:
            case InternalEffectId.Tree:
            case InternalEffectId.WATER:
                vertex.Position = BinaryUtil.ReadVector3(reader);
                vertex.Normal = BinaryUtil.ReadNormal(reader, true);
                vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                vertex.Color = reader.ReadUInt32(); // daytime color
                vertex.Color2 = reader.ReadUInt32(); // nighttime color
                break;
            case InternalEffectId.WorldPrelitShader:
                vertex.Position = BinaryUtil.ReadVector3(reader);
                vertex.Color = reader.ReadUInt32(); // daytime color
                vertex.Color2 = reader.ReadUInt32(); // nighttime color
                vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                break;
            case InternalEffectId.WorldZBiasPrelitShader:
                vertex.Position = BinaryUtil.ReadVector3(reader);
                vertex.Normal = BinaryUtil.ReadNormal(reader, true);
                vertex.Color = reader.ReadUInt32(); // daytime color
                vertex.Color2 = reader.ReadUInt32(); // nighttime color
                vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                break;
            case InternalEffectId.WorldNormalMap:
            case InternalEffectId.GLASS_REFLECTNM:
            case InternalEffectId.WorldRoadShader:
            case InternalEffectId.WorldFEShader:
                vertex.Position = BinaryUtil.ReadVector3(reader);
                vertex.Normal = BinaryUtil.ReadNormal(reader, true);
                vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                vertex.Color = reader.ReadUInt32(); // daytime color
                vertex.Color2 = reader.ReadUInt32(); // nighttime color
                vertex.Tangent = BinaryUtil.ReadNormal(reader, true);
                break;
            case InternalEffectId.CarShader:
            case InternalEffectId.CARNORMALMAP:
                vertex.Position = BinaryUtil.ReadNormal(reader, true) * 8;
                vertex.TexCoords = new Vector2(reader.ReadInt16() / 4096f, reader.ReadInt16() / 4096f - 1);
                vertex.Color = reader.ReadUInt32();
                vertex.Normal = BinaryUtil.ReadNormal(reader, true);
                vertex.Tangent = BinaryUtil.ReadNormal(reader, true);
                break;
            default:
                throw new Exception($"Unsupported effect in object {Solid.Name}: {id}");
        }

        return vertex;
    }

    protected override void ProcessVertices(ref SolidMeshVertex[] vertices, int vertexStreamId)
    {
        // If we don't have a morph list for the stream, bail out
        if (Solid.MorphLists.Count <= vertexStreamId) return;

        var morphList = Solid.MorphLists[vertexStreamId];

        foreach (var morphInfo in morphList)
            for (var i = morphInfo.VertexStartIndex;
                 i <= morphInfo.VertexEndIndex;
                 i++)
                vertices[i].Position = Vector3.Transform(
                    vertices[i].Position,
                    Solid.MorphMatrices[morphInfo.MorphMatrixIndex]);
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct SolidObjectHeader
    {
        public long Padding;

        public uint Padding2;

        public byte Version;
        [MarshalAs(UnmanagedType.I1)] public bool EndianSwapped;
        public ushort Flags;

        public uint Hash;

        public ushort NumTris;
        public ushort NumVerts;

        public byte NumBones;
        public byte NumTextureTableEntries;
        public byte NumLightMaterials;
        public byte NumPositionMarkerTableEntries;

        public uint Padding3;

        public Vector3 BoundsMin;
        public readonly int Blank5;

        public Vector3 BoundsMax;
        public readonly int Blank6;

        public Matrix4x4 Transform;

        public long Padding4, Padding5, Padding6;

        public float Volume, Density;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 52)]
    internal struct SolidObjectDescriptor
    {
        public long Padding;

        public uint Padding2;

        public uint Flags;

        public uint NumMats;

        public uint Zero;

        public uint NumVertexStreams;

        public ulong Padding3;
        public uint Padding4;

        public uint NumTris;

        public uint NumIndices;

        public uint Zero2;
    }

    // 116 bytes
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct Material
    {
        public uint Flags;
        public uint SortKey;
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

    private enum InternalEffectId
    {
        WorldShader,
        WorldZBiasShader,
        WorldNormalMap,
        WorldRoadShader,
        WorldPrelitShader,
        WorldZBiasPrelitShader,
        WorldBoneShader,
        WorldFEShader,
        CarShader,
        CARNORMALMAP,
        GLASS_REFLECT,
        GLASS_REFLECTNM,
        Tree,
        UCAP,
        skyshader,
        WATER
    }
}