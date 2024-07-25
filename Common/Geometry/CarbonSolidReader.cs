using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using Common.Geometry.Data;

namespace Common.Geometry;

public class CarbonSolidReader : SolidReader<CarbonObject, CarbonMaterial>
{
    private int _namedMaterials;

    public CarbonSolidReader()
    {
        Solid = new CarbonObject();
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
            case 0x13401A:
                ReadSolidPositionMarkers(binaryReader, chunkSize);
                break;
            case 0x13401D:
                ReadSolidMorphTargets(binaryReader, chunkSize);
                break;
            case 0x13401F:
                ReadSolidSelectionSets(binaryReader, chunkSize);
                break;
            case 0x134020:
                ReadSolidSelectionSetEdgeLists(binaryReader, chunkSize);
                break;
            case 0x134021:
                ReadSolidSelectionSetEdges(binaryReader, chunkSize);
                break;
            default:
                throw new InvalidDataException(
                    $"Not sure how to handle solid chunk: 0x{chunkId:X} at 0x{binaryReader.BaseStream.Position:X}");
        }
    }

    private void ReadSolidSelectionSetEdges(BinaryReader binaryReader, uint chunkSize)
    {
        // TODO: Do something with this data
        binaryReader.BaseStream.Position += chunkSize;
    }

    private void ReadSolidSelectionSetEdgeLists(BinaryReader binaryReader, uint chunkSize)
    {
        Debug.Assert(chunkSize % 8 == 0, "chunkSize % 8 == 0");
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
            case 0x134b01:
                ReadSolidPlatVertexBuffer(binaryReader, chunkSize);
                break;
            case 0x134b02:
                ReadSolidPlatMeshEntries(binaryReader, chunkSize);
                break;
            case 0x134b03:
                ReadSolidPlatIndices(binaryReader);
                break;
            case 0x134c02:
                ReadSolidPlatMeshEntryName(binaryReader, chunkSize);
                break;
            case 0x134C04:
                ReadUCapFrameWeights(binaryReader, chunkSize);
                break;
            default:
                throw new InvalidDataException(
                    $"Not sure how to handle mesh chunk: 0x{chunkId:X} at 0x{binaryReader.BaseStream.Position:X}");
        }
    }

    private void ReadUCapFrameWeights(BinaryReader binaryReader, uint chunkSize)
    {
        chunkSize -= BinaryUtil.AlignReader(binaryReader, 0x10);
        // TODO: Do something with this data
        binaryReader.BaseStream.Position += chunkSize;
    }

    private void ReadSolidSelectionSets(BinaryReader binaryReader, uint chunkSize)
    {
        Debug.Assert(chunkSize % 16 == 0, "chunkSize % 16 == 0");
        // TODO: Do something with this data
        binaryReader.BaseStream.Position += chunkSize;
    }

    private void ReadSolidMorphTargets(BinaryReader binaryReader, uint chunkSize)
    {
        Debug.Assert(chunkSize % 12 == 0, "chunkSize % 12 == 0");
        var morphTargets = new (uint hash, float blendAmt)[chunkSize / 12];
        for (uint i = 0; i < morphTargets.Length; i++)
        {
            var nameHash = binaryReader.ReadUInt32();
            binaryReader.ReadUInt32();
            var blendAmount = binaryReader.ReadSingle();
            morphTargets[i] = (nameHash, blendAmount);
            Debug.Assert(blendAmount == 0.0f);
            Solid.MorphTargets.Add(nameHash);
        }
    }

    private void ReadSolidPlatMeshEntryName(BinaryReader binaryReader, uint chunkSize)
    {
        if (chunkSize > 0) Solid.Materials[_namedMaterials++].Name = BinaryUtil.ReadNullTerminatedString(binaryReader);
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

    private void ReadSolidPlatVertexBuffer(BinaryReader binaryReader, uint chunkSize)
    {
        chunkSize -= BinaryUtil.AlignReader(binaryReader, 0x10);
        var vb = new byte[chunkSize];

        if (binaryReader.Read(vb, 0, vb.Length) != vb.Length)
            throw new InvalidDataException($"Failed to read {vb.Length} bytes of vertex data");

        VertexBuffers.Add(vb);
    }

    private void ReadSolidPlatMeshEntries(BinaryReader binaryReader, uint chunkSize)
    {
        chunkSize -= BinaryUtil.AlignReader(binaryReader, 0x10);
        Debug.Assert(chunkSize % 144 == 0);
        var numMats = chunkSize / 144;

        var streamIndex = 0;
        var lastEffectId = 0u;

        for (var j = 0; j < numMats; j++)
        {
            var shadingGroup = BinaryUtil.ReadStruct<SolidObjectShadingGroup>(binaryReader);

            if (j > 0 && shadingGroup.EffectId != lastEffectId) streamIndex++;

            var solidObjectMaterial = new CarbonMaterial
            {
                Flags = shadingGroup.Flags,
                NumIndices = shadingGroup.NumTris * 3,
                MinPoint = shadingGroup.BoundsMin,
                MaxPoint = shadingGroup.BoundsMax,
                NumVerts = shadingGroup.NumVerts,
                TextureHash = Solid.TextureHashes[shadingGroup.TextureNumber[0]],
                EffectId = shadingGroup.EffectId,
                VertexSetIndex = streamIndex
            };

            Solid.Materials.Add(solidObjectMaterial);
            NumVertices += shadingGroup.NumVerts;

            lastEffectId = shadingGroup.EffectId;
        }
    }

    private void ReadSolidPlatInfo(BinaryReader binaryReader)
    {
        BinaryUtil.AlignReader(binaryReader, 0x10);
        BinaryUtil.ReadUnmanagedStruct<SolidObjectDescriptor>(binaryReader);
    }

    protected override SolidMeshVertex GetVertex(BinaryReader reader, CarbonMaterial material, int stride)
    {
        var vertex = new SolidMeshVertex();
        var id = (InternalEffectId)material.EffectId;

        switch (id)
        {
            case InternalEffectId.WorldShader:
                vertex.Position = BinaryUtil.ReadVector3(reader);
                vertex.Normal = BinaryUtil.ReadVector3(reader);
                vertex.Color = reader.ReadUInt32();
                vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                break;
            case InternalEffectId.skyshader:
                vertex.Position = BinaryUtil.ReadVector3(reader);
                vertex.Normal = BinaryUtil.ReadVector3(reader);
                vertex.Color = reader.ReadUInt32();
                vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                reader.BaseStream.Position += 8; // TODO: What's this additional D3DDECLUSAGE_TEXCOORD element?
                break;
            case InternalEffectId.WorldNormalMap:
            case InternalEffectId.WorldReflectShader:
                vertex.Position = BinaryUtil.ReadVector3(reader);
                vertex.Normal = BinaryUtil.ReadVector3(reader);
                vertex.Color = reader.ReadUInt32();
                vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                vertex.Tangent = BinaryUtil.ReadVector3(reader);
                reader.BaseStream.Position += 4; // skip W component of tangent vector
                break;
            case InternalEffectId.CarShader:
                vertex.Position = BinaryUtil.ReadVector3(reader);
                vertex.Normal = BinaryUtil.ReadVector3(reader);
                vertex.Color = reader.ReadUInt32();
                vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                vertex.Tangent = BinaryUtil.ReadVector3(reader);
                break;
            case InternalEffectId.CARNORMALMAP:
                vertex.Position = BinaryUtil.ReadVector3(reader);
                vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                vertex.Color = reader.ReadUInt32();
                vertex.Normal = BinaryUtil.ReadVector3(reader);
                vertex.Tangent = BinaryUtil.ReadVector3(reader);
                break;
            case InternalEffectId.GLASS_REFLECT:
                vertex.Position = BinaryUtil.ReadVector3(reader);
                vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                vertex.Color = reader.ReadUInt32();
                vertex.Normal = BinaryUtil.ReadVector3(reader);
                break;
            case InternalEffectId.WATER:
                vertex.Position = BinaryUtil.ReadVector3(reader);
                vertex.Color = reader.ReadUInt32();
                vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                vertex.Normal = BinaryUtil.ReadVector3(reader);
                break;
            case InternalEffectId.WorldBoneShader:
                vertex.Position = BinaryUtil.ReadVector3(reader);
                vertex.Normal = BinaryUtil.ReadVector3(reader);
                vertex.Color = reader.ReadUInt32();
                vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                vertex.BlendWeight = BinaryUtil.ReadVector3(reader);
                vertex.BlendIndices = BinaryUtil.ReadVector3(reader);
                vertex.Tangent = BinaryUtil.ReadVector3(reader);
                break;
            case InternalEffectId.UCAP:
                vertex.Position = BinaryUtil.ReadVector3(reader);
                vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                vertex.BlendWeight = BinaryUtil.ReadVector3(reader);
                vertex.BlendIndices = BinaryUtil.ReadVector3(reader);
                reader.BaseStream.Position += 8 * 16; // TODO: Can we do something with this data? (8x vector4)
                break;
            default:
                throw new Exception($"Unsupported effect in object {Solid.Name}: {id}");
        }

        return vertex;
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

        public readonly int Unknown4;

        public readonly float Unknown5;
        public readonly float Unknown6;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 144)]
    private struct SolidObjectShadingGroup
    {
        public readonly Vector3 BoundsMin; // @0x0

        public readonly Vector3 BoundsMax; // @0xC

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public readonly byte[] TextureNumber; // @0x18

        public readonly byte LightMaterialNumber; // @0x1D

        public readonly ushort Unknown; // @0x1E

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        private readonly byte[] Blank; // @0x20

        public readonly ushort EffectId; // @0x30
        public readonly ushort Unknown2; // @0x32

        public readonly uint Unknown3; // @0x34

        public readonly uint Flags; // @0x38

        public readonly uint TextureSortKey; // @0x3C

        public readonly uint NumVerts; // @0x40
        public readonly uint Unknown4; // @0x44

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        private readonly uint[] Blank2; // @0x48

        public readonly uint NumTris; // @0x60
        public readonly uint IndexOffset; // @0x64

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        private readonly uint[] Blank3; // @0x68

        public readonly uint NumIndices; // @0x7C

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        private readonly uint[] Blank4; // @0x80
        // end @ 0x90
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct SolidObjectDescriptor
    {
        private readonly ulong Padding;

        private readonly uint Unknown1;

        public readonly uint Flags;

        public readonly uint NumMats;

        private readonly uint Blank2;

        public readonly uint NumVertexStreams;

        private readonly ulong Padding2, Padding3;

        public readonly uint NumIndices; // 0 if NumTris
        public readonly uint NumTris; // 0 if NumIndices
    }

    private enum InternalEffectId
    {
        WorldShader,
        WorldReflectShader,
        WorldBoneShader,
        WorldNormalMap,
        CarShader,
        CARNORMALMAP,
        WorldMinShader,
        FEShader,
        FEMaskShader,
        FilterShader,
        ScreenFilterShader,
        RainDropShader,
        VisualTreatmentShader,
        WorldPrelitShader,
        ParticlesShader,
        skyshader,
        shadow_map_mesh,
        CarShadowMapShader,
        WorldDepthShader,
        shadow_map_mesh_depth,
        NormalMapNoFog,
        InstanceMesh,
        ScreenEffectShader,
        HDRShader,
        UCAP,
        GLASS_REFLECT,
        WATER,
        RVMPIP,
        GHOSTCAR
    }
}