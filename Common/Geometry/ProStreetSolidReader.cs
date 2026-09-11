using Common.Geometry.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Common.Geometry;

public class ProStreetSolidReader : SolidReader<ProStreetObject, ProStreetMaterial>
{
    private int _namedMaterials;

    private readonly Dictionary<uint, int> _skipDumpCounts = new();
    private const int MaxSkipDumpVertsPerMaterial = 5;

    private void LogSkipBytes(string tag, uint materialHash, byte[] bytes)
    {
        if (!_skipDumpCounts.TryGetValue(materialHash, out var count)) count = 0;
        if (count >= MaxSkipDumpVertsPerMaterial) return;
        _skipDumpCounts[materialHash] = count + 1;

        var hex = string.Join(" ", bytes.Select(b => b.ToString("X2")));
        var floats = new List<string>();
        for (var i = 0; i + 4 <= bytes.Length; i += 4)
            floats.Add(BitConverter.ToSingle(bytes, i).ToString("F4"));

        File.AppendAllText("prostreet_skip_bytes.txt",
            $"{Solid.Name} | {tag} | materialHash=0x{materialHash:X8} | hex=[{hex}] | asFloats=[{string.Join(", ", floats)}]\n");
    }

    public ProStreetSolidReader()
    {
        Solid = new ProStreetObject();
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
            case 0x134015:
                ReadSolidTextureTypes(binaryReader, chunkSize);
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
            case 0x13401D:
                ReadSolidMorphTargets(binaryReader, chunkSize);
                break;
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

    private void ReadSolidTextureTypes(BinaryReader binaryReader, uint chunkSize)
    {
        // TODO: Do something with this data
        binaryReader.BaseStream.Position += chunkSize;
    }

    private void ReadSolidHeader(BinaryReader binaryReader)
    {
        BinaryUtil.AlignReader(binaryReader, 0x10);
        var header = BinaryUtil.ReadStruct<SolidObjectHeader>(binaryReader);
        Debug.Assert(header.Version == 0x19, "header.Version == 0x19");
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
            // case 0x134C04:
            //     ReadUCapFrameWeights(binaryReader, chunkSize);
            //     break;
            default:
                throw new InvalidDataException(
                    $"Not sure how to handle mesh chunk: 0x{chunkId:X} at 0x{binaryReader.BaseStream.Position:X}");
        }
    }

    private void ReadSolidPlatMeshEntryName(BinaryReader binaryReader, uint chunkSize)
    {
        if (chunkSize > 0) Solid.Materials[_namedMaterials++].Name = BinaryUtil.ReadNullTerminatedString(binaryReader);
    }

    private void ReadSolidPlatIndices(BinaryReader binaryReader)
    {
        BinaryUtil.AlignReader(binaryReader, 0x20);
        foreach (var material in Solid.Materials)
        {
            material.Indices = new ushort[material.NumIndices];
            for (var j = 0; j < material.NumIndices; j++) material.Indices[j] = binaryReader.ReadUInt16();
        }
    }

    private void ReadSolidPlatMeshEntries(BinaryReader binaryReader, uint chunkSize)
    {
        chunkSize -= BinaryUtil.AlignReader(binaryReader, 0x80);
        var shadingGroupSize = Marshal.SizeOf<SolidObjectShadingGroup>();
        Debug.Assert(chunkSize % shadingGroupSize == 0);
        var numMats = chunkSize / shadingGroupSize;

        for (var j = 0; j < numMats; j++)
        {
            var shadingGroup = BinaryUtil.ReadStruct<SolidObjectShadingGroup>(binaryReader);

            //var rawSlots = string.Join(",", shadingGroup.TextureShaderUsage.Select(b =>
            //  b < Solid.TextureHashes.Count
            //    ? $"{b}->0x{Solid.TextureHashes[b]:X8}"
            //  : $"{b}->OUTOFRANGE"));
            //File.AppendAllText("prostreet_raw_slots.txt",
            //$"{Solid.Name} | matIndex={j} | effect={ProStreetSolidReader.GetEffectName(shadingGroup.EffectId)} | rawSlots=[{rawSlots}]\n");

            Solid.Materials.Add(new ProStreetMaterial
            {
                Flags = shadingGroup.Flags,
                NumIndices = shadingGroup.IndicesUsed,
                NumVerts = shadingGroup.VertexBufferUsage / shadingGroup.Flags2[2],
                VertexSetIndex = j,
                Hash = shadingGroup.UnknownId,
                DiffuseTextureHash = Solid.TextureHashes[shadingGroup.TextureShaderUsage[4]],
                EffectId = shadingGroup.EffectId,
                TextureHashes = shadingGroup.TextureShaderUsage
                    .Where(idx => idx < Solid.TextureHashes.Count)
                    .Select(idx => Solid.TextureHashes[idx])
                    .Where(hash => hash != 0)
                    .Distinct()
                    .ToArray()
            });

            NumVertices += shadingGroup.VertexBufferUsage / shadingGroup.Flags2[2];
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
        BinaryUtil.ReadUnmanagedStruct<SolidObjectDescriptor>(binaryReader);
    }

    private readonly HashSet<uint> _loggedUvGapMaterials = new();

    #region PROSTREET_DIAG_SKIPBYTES
    private readonly Dictionary<uint, int> _diagSkipDumpCounts = new();
    private const int DiagMaxSkipVertsPerMaterial = 8;

    private void DiagLogSkipBytes(string tag, uint materialHash, int textureCount, byte[] bytes)
    {
        if (!_diagSkipDumpCounts.TryGetValue(materialHash, out var count)) count = 0;
        if (count >= DiagMaxSkipVertsPerMaterial) return;
        _diagSkipDumpCounts[materialHash] = count + 1;

        var hex = string.Join(" ", bytes.Select(b => b.ToString("X2")));
        var floats = new List<float>();
        for (var i = 0; i + 4 <= bytes.Length; i += 4)
            floats.Add(BitConverter.ToSingle(bytes, i));

        var pairTags = new List<string>();
        for (var i = 0; i + 1 < floats.Count; i += 2)
        {
            var u = floats[i];
            var v = floats[i + 1];
            var looksLikeUv = Math.Abs(u) < 4f && Math.Abs(v) < 4f && !float.IsNaN(u) && !float.IsNaN(v);
            pairTags.Add($"({u:F4}, {v:F4}){(looksLikeUv ? " <-- UV-CANDIDATE" : "")}");
        }

        File.AppendAllText("prostreet_diag_skipbytes.txt",
            $"{Solid.Name} | {tag} | materialHash=0x{materialHash:X8} | texCount={textureCount} | hex=[{hex}] | pairs=[{string.Join(", ", pairTags)}]\n");
    }
    #endregion
    protected override SolidMeshVertex GetVertex(BinaryReader reader, ProStreetMaterial material, int stride)
    {
        var psm = (ProStreetMaterial)material;
        var vertex = new SolidMeshVertex();
        var startPos = reader.BaseStream.Position;

        var effectId = (EffectId)psm.EffectId;
        switch (effectId)
        {
            case EffectId.WORLDBAKEDLIGHTING:
                vertex.Position = BinaryUtil.ReadVector3(reader);
                vertex.Color = reader.ReadUInt32();
                vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                reader.BaseStream.Position += 16;
                vertex.Normal = BinaryUtil.ReadNormal(reader, true);
                break;
            case EffectId.WORLD:
                vertex.Position = BinaryUtil.ReadVector3(reader);
                vertex.Normal = BinaryUtil.ReadNormal(reader);
                vertex.Color = reader.ReadUInt32();
                vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                vertex.TexCoords1 = BinaryUtil.ReadVector2(reader);
                break;
            case EffectId.SKY:
                vertex.Position = BinaryUtil.ReadVector3(reader);
                vertex.Normal = BinaryUtil.ReadNormal(reader);
                vertex.Color = reader.ReadUInt32();
                vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                reader.BaseStream.Position += 8; // TODO: what is this second D3DDECLUSAGE_TEXCOORD element?
                break;
            case EffectId.WORLDNORMALMAP:
                vertex.Position = BinaryUtil.ReadVector3(reader);
                vertex.Normal = BinaryUtil.ReadNormal(reader);
                vertex.Color = reader.ReadUInt32();
                vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                reader.BaseStream.Position += 8; // TODO: what is this second D3DDECLUSAGE_TEXCOORD element?
                // TODO: read tangent
                reader.BaseStream.Position += 16;
                reader.BaseStream.Position +=
                    8; // TODO: what are these other D3DDECLUSAGE_TEXCOORD elements? (2x D3DDECLTYPE_UBYTE4)
                break;
            case EffectId.WorldDepthShader:
                vertex.Position = BinaryUtil.ReadVector3(reader);
                vertex.Normal = BinaryUtil.ReadNormal(reader);
                vertex.Color = reader.ReadUInt32();
                vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                break;
            case EffectId.TREELEAVES:
                vertex.Position = BinaryUtil.ReadVector3(reader);
                vertex.Color = reader.ReadUInt32();
                vertex.Normal = BinaryUtil.ReadNormal(reader);
                // TODO: COLLADA supports tangent vectors, so we should eventually read them
                //> elements[3]: type=D3DDECLTYPE_FLOAT4 usage=D3DDECLUSAGE_TANGENT size=16 offset=0x1c
                reader.BaseStream.Position += 0x10;
                vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                break;
            case EffectId.WORLDCONSTANT:
                vertex.Position = BinaryUtil.ReadVector3(reader);
                vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                vertex.Color = reader.ReadUInt32();
                break;
            case EffectId.ROAD:
                // shaderusages_legacy.txt shader 18: Pos0f3_Tex0_Tex1_Tex2_xb4_xb4_Norm0s [52]
                // Norm0s decode is unreliable (byte order within the last 16 bytes unconfirmed) -
                // skip it for now and let normals be generated downstream instead of exporting garbage.
                // TextureUsages=ROADBASE,ROADELEMENTS,ROADDAMAGE,DETAILMAP - no vertex color at all.
                vertex.Position = BinaryUtil.ReadVector3(reader);
                vertex.TexCoords = BinaryUtil.ReadVector2(reader);   // Tex0 (ROADBASE)
                vertex.TexCoords1 = BinaryUtil.ReadVector2(reader);  // Tex1 (ROADELEMENTS, e.g. lines)
                vertex.TexCoords2 = BinaryUtil.ReadVector2(reader);  // Tex2 (ROADDAMAGE)
                reader.BaseStream.Position += 16; // xb4 (4) + xb4 (4) + Norm0s (8, unreliable, skipped for now)
                break;
            case EffectId.TERRAIN:
                vertex.Position = BinaryUtil.ReadVector3(reader);
                vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                reader.BaseStream.Position += 16;
                vertex.Normal = BinaryUtil.ReadNormal(reader, true);
                reader.BaseStream.Position += 8;
                vertex.Color = reader.ReadUInt32();
                reader.BaseStream.Position += 8;
                break;
            case EffectId.GRASSTERRAIN:
                // shaderusages_legacy.txt shader 12: Pos0f3_Tex0_Tex1_Tex2_xb4_Norm0s_Tan0s_Col0 [60]
                // TextureUsages=TERRAINONE,TERRAINTWO,NORMALMAP,NORMALMAP2 - two real extra UV
                // channels (Tex1/Tex2), one per terrain diffuse layer.
                vertex.Position = BinaryUtil.ReadVector3(reader);
                vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                vertex.TexCoords1 = BinaryUtil.ReadVector2(reader);
                vertex.TexCoords2 = BinaryUtil.ReadVector2(reader);
                vertex.Unknown_xb4 = reader.ReadBytes(4);
                vertex.Normal = BinaryUtil.ReadNormal(reader, true);
                vertex.Unknown_Tan0s = reader.ReadBytes(8);          // Tan0s - was skipped, now captured raw
                vertex.Color = reader.ReadUInt32();
                break;
            case EffectId.FLAG:
                vertex.Position = BinaryUtil.ReadVector3(reader);
                vertex.Color = reader.ReadUInt32();
                vertex.Normal = BinaryUtil.ReadNormal(reader, true);
                vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                reader.BaseStream.Position += 8; // TODO: what is this second D3DDECLUSAGE_TEXCOORD element?
                break;
            case EffectId.GRASSCARD:
                vertex.Position = BinaryUtil.ReadVector3(reader);
                vertex.Color = reader.ReadUInt32();
                vertex.Normal = BinaryUtil.ReadNormal(reader, true);
                // TODO: read packed tangent vector (4 short-components)
                reader.BaseStream.Position += 8;
                vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                reader.BaseStream.Position += 8; // TODO: what is this second D3DDECLUSAGE_TEXCOORD element?
                break;
            case EffectId.CAR:
            case EffectId.CARNORMALMAP:
            case EffectId.CARVINYL:
                vertex.Position = BinaryUtil.ReadNormal(reader, true);
                vertex.TexCoords = BinaryUtil.ReadShort2N(reader);
                vertex.Color = reader.ReadUInt32();
                vertex.Normal = BinaryUtil.ReadNormal(reader, true);
                vertex.Tangent = BinaryUtil.ReadNormal(reader, true);
                break;
            case EffectId.ALWAYSFACING:
                vertex.Position = BinaryUtil.ReadVector3(reader);
                vertex.Color = reader.ReadUInt32();
                vertex.Normal = BinaryUtil.ReadNormal(reader, true);
                // TODO: read packed tangent vector (4 short-components)
                reader.BaseStream.Position += 8;
                vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                break;
            case EffectId.STANDARD:
                vertex.Position = BinaryUtil.ReadVector3(reader);
                vertex.Color = reader.ReadUInt32();
                vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                reader.BaseStream.Position += 8; // todo: what's this?
                break;
            case EffectId.TUNNEL:
                // shaderusages_legacy.txt shader 29: Pos0f3_Tex0_Tex1_xb4_xb4_Col0_Norm0s [48]
                // "tunnel walls 7F7F7FFF is color, xb4 80808080"
                vertex.Position = BinaryUtil.ReadVector3(reader);
                vertex.TexCoords = BinaryUtil.ReadVector2(reader);   // Tex0
                vertex.TexCoords1 = BinaryUtil.ReadVector2(reader);  // Tex1
                reader.BaseStream.Position += 4; // xb4 (unknown)
                reader.BaseStream.Position += 4; // xb4 (observed 80808080)
                vertex.Color = reader.ReadUInt32(); // Col0
                vertex.Normal = BinaryUtil.ReadNormal(reader, true); // Norm0s
                break;
            case EffectId.ROADLIGHTMAP:
                // shaderusages_legacy.txt shader 19: Pos0f3_Tex0_Tex1_Tex2_xb4_Norm0s_Col0 [52]
                // TextureUsages=ROADBASE,ROADELEMENTS,LIGHTMAP,DETAILMAP - "tunnel road xb4 80808080"
                vertex.Position = BinaryUtil.ReadVector3(reader);
                vertex.TexCoords = BinaryUtil.ReadVector2(reader);   // Tex0 (ROADBASE)
                vertex.TexCoords1 = BinaryUtil.ReadVector2(reader);  // Tex1 (ROADELEMENTS)
                vertex.TexCoords2 = BinaryUtil.ReadVector2(reader);  // Tex2 (LIGHTMAP)
                reader.BaseStream.Position += 4; // xb4 (observed 80808080)
                vertex.Normal = BinaryUtil.ReadNormal(reader, true); // Norm0s
                vertex.Color = reader.ReadUInt32(); // Col0
                break;
            case EffectId.WATER:
                vertex.Position = BinaryUtil.ReadVector3(reader);
                vertex.Color = reader.ReadUInt32();
                vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                break;
            case EffectId.WORLDBONE:
                vertex.Position = BinaryUtil.ReadVector3(reader);
                vertex.Normal = BinaryUtil.ReadNormal(reader);
                vertex.Color = reader.ReadUInt32();
                vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                vertex.BlendWeight = BinaryUtil.ReadVector3(reader);
                vertex.BlendIndices = BinaryUtil.ReadVector3(reader);
                vertex.Tangent = BinaryUtil.ReadVector3(reader);
                break;
            case EffectId.SMOKE:
                vertex.Position = BinaryUtil.ReadVector3(reader);
                vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                vertex.Normal = BinaryUtil.ReadNormal(reader, true);
                break;
            default:
                throw new Exception($"Unsupported effect in object {Solid.Name}: {effectId}");
        }
        // var consumed = (int)(reader.BaseStream.Position - startPos);
        //var leftover = stride - consumed;
        //if (leftover > 0 && _loggedUvGapMaterials.Add(psm.Hash))
        //File.AppendAllText("prostreet_uv_gaps.txt",
        //    $"{Solid.Name} | effect={GetEffectName(psm.EffectId)} | materialHash=0x{psm.Hash:X8} | stride={stride} consumed={consumed} leftover={leftover}\n");

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

        public readonly ushort NumTris;
        public readonly ushort NumVerts;

        public readonly byte NumBones;
        public readonly byte NumTextureTableEntries;
        public readonly byte NumLightMaterials;
        public readonly byte NumPositionMarkerTableEntries;

        public readonly uint Blank2;

        public readonly Vector3 BoundsMin;
        public readonly int Blank5;

        public readonly Vector3 BoundsMax;
        public readonly int Blank6;

        public readonly Matrix4x4 Transform;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public readonly uint[] Unknown3;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public readonly uint[] Unknown4;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct SolidObjectShadingGroup
    {
        // begin header
        public readonly uint FirstIndex;

        public readonly uint EffectId;

        public readonly uint Blank;

        public readonly uint Unknown2;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public readonly uint[] Unknown3;

        // end header

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public readonly byte[] TextureShaderUsage;

        public readonly uint Unknown4;

        public readonly uint UnknownId;
        public readonly uint Flags;
        public readonly uint IndicesUsed;
        public readonly uint Unknown5;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public readonly uint[] Blank2;

        public readonly uint VertexBufferUsage; // / 0x20

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public readonly byte[] Flags2;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public readonly uint[] Blank3;

        public readonly uint Unknown6;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct SolidObjectDescriptor
    {
        private readonly ulong Padding;

        public readonly uint Unknown1;

        public readonly uint Flags;

        public readonly uint NumMats;

        public readonly uint Blank2;

        public readonly uint NumVertexStreams; // should be 0, real count == MaterialShaderCount

        public readonly ulong Padding2;

        public readonly uint Padding3;

        public readonly uint NumTris; // 0 if NumTriIndices

        public readonly uint NumIndices; // 0 if NumTris

        public readonly uint Unknown2;

        public readonly uint Blank4;
    }

    private readonly Dictionary<uint, int> _colorDumpCounts = new();
    private const int MaxColorDumpVertsPerMaterial = 25;

    private void LogVertexColor(string tag, uint materialHash, uint color)
    {
        if (!_colorDumpCounts.TryGetValue(materialHash, out var count)) count = 0;
        if (count >= MaxColorDumpVertsPerMaterial) return;
        _colorDumpCounts[materialHash] = count + 1;

        var a = (color >> 24) & 0xFF;
        var r = (color >> 16) & 0xFF;
        var g = (color >> 8) & 0xFF;
        var b = color & 0xFF;

        File.AppendAllText("prostreet_vertex_colors.txt",
            $"{Solid.Name} | {tag} | materialHash=0x{materialHash:X8} | argb=0x{color:X8} | A={a} R={r} G={g} B={b}\n");
    }

    public static string GetEffectName(uint effectId) =>
    Enum.IsDefined(typeof(EffectId), (int)effectId) ? ((EffectId)effectId).ToString() : $"0x{effectId:X}";
    private enum EffectId
    {
        STANDARD,
        TREELEAVES,
        WORLD,
        WORLDBONE,
        WORLDNORMALMAP,
        CARNORMALMAP,
        CARVINYL,
        CAR,
        VISUAL_TREATMENT,
        PARTICLES,
        SKY,
        GRASSCARD,
        GRASSTERRAIN,
        WorldDepthShader,
        ScreenEffectShader,
        UCAP,
        WATER,
        GHOSTCAR,
        ROAD,
        ROADLIGHTMAP,
        WORLDCONSTANT,
        TERRAIN,
        WORLDBAKEDLIGHTING,
        SHADOWMAPMESH,
        DEBUGPOLY,
        CROWD,
        WORLDDECAL,
        SMOKE,
        FLAG,
        TUNNEL,
        WORLDENVIROMAP,
        ALWAYSFACING
    }
}