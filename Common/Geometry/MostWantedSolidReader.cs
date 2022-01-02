using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using Common.Geometry.Data;

namespace Common.Geometry;

public class MostWantedSolidReader : SolidReader<MostWantedObject>
{
    private int _namedMaterials;

    public MostWantedSolidReader()
    {
        Solid = new MostWantedObject();
    }

    protected override void ProcessSolidChunk(BinaryReader binaryReader, uint chunkId,
        uint chunkSize)
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
                ReadSolidNormalSmoother(binaryReader);
                break;
            case 0x134018:
                ReadSolidSmoothVertices(binaryReader, chunkSize);
                break;
            case 0x134019:
                ReadSolidSmoothVerticesPlat(binaryReader, chunkSize);
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
        var name = BinaryUtil.ReadNullTerminatedString(binaryReader);

        Solid.Name = name;
        Solid.Hash = header.Hash;
        Solid.MinPoint = header.BoundsMin;
        Solid.MaxPoint = header.BoundsMax;
        Solid.Transform = header.Transform;
    }

    protected override void ProcessPlatChunk(BinaryReader binaryReader, uint chunkId,
        uint chunkSize)
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
            default:
                throw new InvalidDataException(
                    $"Not sure how to handle mesh chunk: 0x{chunkId:X} at 0x{binaryReader.BaseStream.Position:X}");
        }
    }

    private void ReadSolidSmoothVerticesPlat(BinaryReader binaryReader, uint chunkSize)
    {
        Debug.Assert(chunkSize % 12 == 0, "chunkSize % 12 == 0");
    }

    private void ReadSolidSmoothVertices(BinaryReader binaryReader, uint chunkSize)
    {
        Debug.Assert(chunkSize % 8 == 0, "chunkSize % 8 == 0");
    }

    private void ReadSolidNormalSmoother(BinaryReader binaryReader)
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

    private void ReadSolidPlatVertexBuffer(BinaryReader binaryReader, uint chunkSize)
    {
        chunkSize -= BinaryUtil.AlignReader(binaryReader, 0x80);
        var vb = new byte[chunkSize];

        if (binaryReader.Read(vb, 0, vb.Length) != vb.Length)
            throw new InvalidDataException($"Failed to read {vb.Length} bytes of vertex data");

        VertexBuffers.Add(vb);
    }

    private void ReadSolidPlatMeshEntries(BinaryReader binaryReader, uint chunkSize)
    {
        chunkSize -= BinaryUtil.AlignReader(binaryReader, 0x10);
        var shadingGroupSize = Marshal.SizeOf<SolidObjectShadingGroup>();
        Debug.Assert(chunkSize % shadingGroupSize == 0);
        var numMats = chunkSize / shadingGroupSize;

        var streamIndex = 0;
        var lastEffectId = 0u;

        for (var j = 0; j < numMats; j++)
        {
            var shadingGroup = BinaryUtil.ReadStruct<SolidObjectShadingGroup>(binaryReader);

            if (j > 0 && shadingGroup.EffectId != lastEffectId) streamIndex++;

            var solidObjectMaterial = new MostWantedMaterial
            {
                Flags = shadingGroup.Flags,
                NumIndices = shadingGroup.NumTris * 3,
                MinPoint = shadingGroup.BoundsMin,
                MaxPoint = shadingGroup.BoundsMax,
                NumVerts = shadingGroup.NumVerts,
                TextureHash = Solid.TextureHashes[shadingGroup.TextureNumber[0]],
                EffectId = shadingGroup.EffectId,
                VertexStreamIndex = streamIndex
            };

            Solid.Materials.Add(solidObjectMaterial);

            MeshDescriptor.NumVerts += shadingGroup.NumVerts;
            lastEffectId = shadingGroup.EffectId;
        }
    }

    private void ReadSolidPlatInfo(BinaryReader binaryReader)
    {
        BinaryUtil.AlignReader(binaryReader, 0x10);
        var descriptor = BinaryUtil.ReadUnmanagedStruct<SolidObjectDescriptor>(binaryReader);

        MeshDescriptor = new SolidMeshDescriptor
        {
            Flags = descriptor.Flags,
            HasNormals = true,
            NumIndices = descriptor.NumIndices,
            NumMats = descriptor.NumMats,
            NumVertexStreams = descriptor.NumVertexStreams
        };
    }

    private void ReadSolidPlatMeshEntryName(BinaryReader binaryReader, uint chunkSize)
    {
        if (chunkSize > 0) Solid.Materials[_namedMaterials++].Name = BinaryUtil.ReadNullTerminatedString(binaryReader);
    }

    protected override SolidMeshVertex GetVertex(BinaryReader reader, SolidObjectMaterial material, int stride)
    {
        var mwm = (MostWantedMaterial)material;
        var vertex = new SolidMeshVertex();

        var id = (InternalEffectId)mwm.EffectId;

        switch (id)
        {
            case InternalEffectId.WorldNormalMap:
            case InternalEffectId.WorldReflectShader:
                vertex.Position = BinaryUtil.ReadVector3(reader);
                vertex.Normal = BinaryUtil.ReadVector3(reader);
                vertex.Color = reader.ReadUInt32();
                vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                reader.BaseStream.Position += 8; // TODO: What's this additional D3DDECLUSAGE_TEXCOORD element?
                vertex.Tangent = BinaryUtil.ReadVector3(reader);
                reader.BaseStream.Position += 4; // skip W component of tangent vector
                break;
            case InternalEffectId.skyshader:
                vertex.Position = BinaryUtil.ReadVector3(reader);
                vertex.Normal = BinaryUtil.ReadVector3(reader);
                vertex.Color = reader.ReadUInt32();
                vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                reader.BaseStream.Position += 8; // TODO: What's this additional D3DDECLUSAGE_TEXCOORD element?
                break;
            case InternalEffectId.WorldShader:
            case InternalEffectId.GlossyWindow:
            case InternalEffectId.billboardshader:
            case InternalEffectId.CarShader:
                vertex.Position = BinaryUtil.ReadVector3(reader);
                vertex.Normal = BinaryUtil.ReadVector3(reader);
                vertex.Color = reader.ReadUInt32();
                vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                break;
            case InternalEffectId.WorldBoneShader:
                vertex.Position = BinaryUtil.ReadVector3(reader);
                vertex.Normal = BinaryUtil.ReadVector3(reader);
                vertex.Color = reader.ReadUInt32();
                vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                vertex.BlendWeight = BinaryUtil.ReadVector3(reader);
                vertex.BlendIndices = BinaryUtil.ReadVector3(reader);
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

    private enum InternalEffectId
    {
        WorldShader,
        WorldReflectShader,
        WorldBoneShader,
        WorldNormalMap,
        CarShader,
        GlossyWindow,
        billboardshader,
        WorldMinShader,
        WorldNoFogShader,
        FEShader,
        FEMaskShader,
        FilterShader,
        OverbrightShader,
        ScreenFilterShader,
        RainDropShader,
        RunwayLightShader,
        VisualTreatmentShader,
        WorldPrelitShader,
        ParticlesShader,
        skyshader,
        shadow_map_mesh,
        SkyboxCurrentGen,
        ShadowPolyCurrentGen,
        CarShadowMapShader,
        WorldDepthShader,
        WorldNormalMapDepth,
        CarShaderDepth,
        GlossyWindowDepth,
        TreeDepthShader,
        shadow_map_mesh_depth,
        NormalMapNoFog
    }
}