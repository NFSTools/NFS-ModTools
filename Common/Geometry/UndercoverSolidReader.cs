using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Common.Geometry.Data;

namespace Common.Geometry;

public class UndercoverSolidReader : SolidReader<UndercoverObject, UndercoverMaterial>
{
    private static readonly Dictionary<uint, UndercoverEffectId> EffectIdMapping =
        new()
        {
            { 0x01747d6f, UndercoverEffectId.mw2_diffuse_spec },
            { 0x029546b2, UndercoverEffectId.ubereffectblend },
            { 0x05ff7435, UndercoverEffectId.standardeffect },
            { 0x06b7ad24, UndercoverEffectId.watersplash },
            { 0x073b344f, UndercoverEffectId.mw2_ocean },
            { 0x09f8b274, UndercoverEffectId.mw2_glass_refl },
            { 0x0a19d914, UndercoverEffectId.mw2_glass_no_n },
            { 0x0ae016e3, UndercoverEffectId.worldbone },
            { 0x0cad599e, UndercoverEffectId.mw2_tunnel_road },
            { 0x0ddd6c5c, UndercoverEffectId.car_si_a },
            { 0x0f87f3b6, UndercoverEffectId.worldbonetransparency },
            { 0x117b614f, UndercoverEffectId.worldbone },
            { 0x13b4af33, UndercoverEffectId.worldbone },
            { 0x170e6ebf, UndercoverEffectId.mw2_car_heaven },
            { 0x18c7652d, UndercoverEffectId.mw2_combo_refl },
            { 0x19729193, UndercoverEffectId.mw2_road_lite },
            { 0x19b330d9, UndercoverEffectId.mw2_cardebris },
            { 0x1ad21fd4, UndercoverEffectId.mw2_road_refl_lite },
            { 0x1b801884, UndercoverEffectId.mw2_scrub_lod },
            { 0x1ecc6c4e, UndercoverEffectId.mw2_car_heaven_default },
            { 0x1f264952, UndercoverEffectId.worldbone },
            { 0x2466ee21, UndercoverEffectId.standardeffect },
            { 0x24be0f6e, UndercoverEffectId.mw2_road_refl_lite },
            { 0x266e34fe, UndercoverEffectId.mw2_grass_dirt },
            { 0x275232ed, UndercoverEffectId.diffuse_spec_2sided },
            { 0x2a872bf4, UndercoverEffectId.worldbonenocull },
            { 0x2d3b0296, UndercoverEffectId.mw2_road_refl },
            { 0x2eb62de1, UndercoverEffectId.car_a },
            { 0x308cd669, UndercoverEffectId.mw2_texture_scroll },
            { 0x3242d31a, UndercoverEffectId.mw2_diffuse_spec },
            { 0x354f479d, UndercoverEffectId.worldbonenocull },
            { 0x35aa265b, UndercoverEffectId.mw2_road_refl_tile },
            { 0x361cbdfa, UndercoverEffectId.standardeffect },
            { 0x3620b527, UndercoverEffectId.mw2_icon },
            { 0x3655d45f, UndercoverEffectId.mw2_normalmap },
            { 0x39ffaf87, UndercoverEffectId.mw2_scrub },
            { 0x3d2276dc, UndercoverEffectId.mw2_diffuse_spec_alpha },
            { 0x3fccbbf5, UndercoverEffectId.car_nm },
            { 0x41fc3e7c, UndercoverEffectId.mw2_combo_refl },
            { 0x42b42128, UndercoverEffectId.mw2_matte },
            { 0x47816d49, UndercoverEffectId.mw2_diffuse_spec_alpha },
            { 0x4c42e398, UndercoverEffectId.worldbone },
            { 0x4c4b3f1d, UndercoverEffectId.mw2_pano },
            { 0x4ce1dd43, UndercoverEffectId.car_t_nm },
            { 0x4e479629, UndercoverEffectId.mw2_rock },
            { 0x4f5f8200, UndercoverEffectId.shadowmesh },
            { 0x54f5a555, UndercoverEffectId.car_a_nzw },
            { 0x56a2b9d4, UndercoverEffectId.mw2_normalmap },
            { 0x572610ed, UndercoverEffectId.mw2_diffuse_spec },
            { 0x5797ec4b, UndercoverEffectId.worldbone },
            { 0x5c759d61, UndercoverEffectId.mw2_normalmap },
            { 0x5d5d135a, UndercoverEffectId.mw2_illuminated },
            { 0x62b474b4, UndercoverEffectId.mw2_diffuse_spec },
            { 0x645c60e6, UndercoverEffectId.standardeffect },
            { 0x64b9573b, UndercoverEffectId.mw2_tunnel_road },
            { 0x64f0aba2, UndercoverEffectId.car_v },
            { 0x67a95199, UndercoverEffectId.mw2_constant },
            { 0x698bb218, UndercoverEffectId.mw2_road_refl_tile },
            { 0x6f837ecd, UndercoverEffectId.mw2_constant_alpha_bias },
            { 0x705c7fa1, UndercoverEffectId.normalmap2sided },
            { 0x7155733b, UndercoverEffectId.ubereffect },
            { 0x71770ad7, UndercoverEffectId.car_nm_a },
            { 0x7773651b, UndercoverEffectId.mw2_diffuse_spec },
            { 0x7acc11b2, UndercoverEffectId.mw2_road_refl_overlay },
            { 0x7b83a181, UndercoverEffectId.mw2_constant },
            { 0x7dac986f, UndercoverEffectId.mw2_road },
            { 0x841dd0aa, UndercoverEffectId.mw2_tunnel_wall },
            { 0x87a1a883, UndercoverEffectId.mw2_branches },
            { 0x89ad50b6, UndercoverEffectId.mw2_sky },
            { 0x8a66ab20, UndercoverEffectId.mw2_tunnel_illum },
            { 0x8ad4dfdc, UndercoverEffectId.mw2_ocean },
            { 0x8e0d6a1b, UndercoverEffectId.mw2_foliage },
            { 0x923f903c, UndercoverEffectId.mw2_grass_rock },
            { 0x92510617, UndercoverEffectId.ubereffect },
            { 0x9303b776, UndercoverEffectId.worldbone },
            { 0x97745fa6, UndercoverEffectId.mw2_road_tile },
            { 0x97875314, UndercoverEffectId.mw2_road_lite },
            { 0x99f1e4f0, UndercoverEffectId.mw2_matte_alpha },
            { 0x9c215ff1, UndercoverEffectId.mw2_road_overlay },
            { 0x9d96c16f, UndercoverEffectId.mw2_smokegeo },
            { 0x9db7d630, UndercoverEffectId.mw2_texture_scroll },
            { 0xa063c459, UndercoverEffectId.worldbone },
            { 0xa13753eb, UndercoverEffectId.car },
            { 0xa5356b04, UndercoverEffectId.mw2_matte_alpha },
            { 0xa554e057, UndercoverEffectId.mw2_texture_scroll },
            { 0xa60c8560, UndercoverEffectId.worldbone },
            { 0xa8a62dc5, UndercoverEffectId.car_nm_v_s },
            { 0xab704872, UndercoverEffectId.car_t_a },
            { 0xab7d3ce6, UndercoverEffectId.worldbonetransparency },
            { 0xadc5a4c1, UndercoverEffectId.mw2_normalmap_bias },
            { 0xafec11b7, UndercoverEffectId.mw2_dirt },
            { 0xb25c1588, UndercoverEffectId.car_si },
            { 0xb5fdc966, UndercoverEffectId.worldbonetransparency },
            { 0xb8b7e7fd, UndercoverEffectId.standardeffect },
            { 0xbbf81161, UndercoverEffectId.mw2_trunk },
            { 0xbcd89ccc, UndercoverEffectId.mw2_ocean },
            { 0xbfd62731, UndercoverEffectId.mw2_road },
            { 0xc1368b6a, UndercoverEffectId.car_nm_v_s_a },
            { 0xc146dcc6, UndercoverEffectId.mw2_diffuse_spec_salpha },
            { 0xc7bf7382, UndercoverEffectId.mw2_fol_alwaysfacing },
            { 0xc808bd33, UndercoverEffectId.worldbonetransparency },
            { 0xcc72efc6, UndercoverEffectId.mw2_road_refl },
            { 0xcca7bd31, UndercoverEffectId.mw2_dif_spec_a_bias },
            { 0xd34aa470, UndercoverEffectId.mw2_illuminated },
            { 0xd3865634, UndercoverEffectId.mw2_rock_overlay },
            { 0xd3c214f5, UndercoverEffectId.worldbonetransparency },
            { 0xd5289f76, UndercoverEffectId.mw2_diffuse_spec_alpha },
            { 0xd5912485, UndercoverEffectId.worldbone },
            { 0xd60bdb29, UndercoverEffectId.mw2_road_refl_overlay },
            { 0xd9cd6dbb, UndercoverEffectId.mw2_indicator },
            { 0xdc77b9a2, UndercoverEffectId.mw2_carhvn_floor },
            { 0xe643efd6, UndercoverEffectId.mw2_foliage_lod },
            { 0xe6e487ce, UndercoverEffectId.mw2_dirt_rock },
            { 0xe8983e20, UndercoverEffectId.mw2_diffuse_spec_illum },
            { 0xe9abeaa2, UndercoverEffectId.worldbonetransparency },
            { 0xee3e7019, UndercoverEffectId.mw2_dirt_overlay },
            { 0xf0598a3f, UndercoverEffectId.car_t },
            { 0xf0b14117, UndercoverEffectId.mw2_normalmap },
            { 0xf20a36b0, UndercoverEffectId.mw2_grass },
            { 0xf2e4a987, UndercoverEffectId.mw2_road_overlay },
            { 0xf2e790ae, UndercoverEffectId.mw2_matte },
            { 0xf54fe578, UndercoverEffectId.mw2_parallax },
            { 0xf642075f, UndercoverEffectId.worldbone },
            { 0xf8105027, UndercoverEffectId.mw2_diffuse_spec },
            { 0xfddf0f66, UndercoverEffectId.mw2_road_tile },
            { 0xff37175c, UndercoverEffectId.worldbonenocull },
            { 0, UndercoverEffectId.none }
        };

    private int _namedMaterials;

    public UndercoverSolidReader()
    {
        Solid = new UndercoverObject();
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
            case 0x13401E:
                ReadSolidMorphChildren(binaryReader, chunkSize);
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

    private void ReadSolidMorphChildren(BinaryReader binaryReader, uint chunkSize)
    {
        Debug.Assert(chunkSize % 16 == 0, "chunkSize % 16 == 0");
        // TODO: Do something with this data
        binaryReader.BaseStream.Position += chunkSize;
    }

    private void ReadSolidTextureTypes(BinaryReader binaryReader, uint chunkSize)
    {
        // TODO: Do something with this data
        binaryReader.BaseStream.Position += chunkSize;
    }

    private new void ReadSolidTextures(BinaryReader binaryReader, uint chunkSize)
    {
        Debug.Assert(chunkSize % 12 == 0, "chunkSize % 12 == 0");
        for (var j = 0; j < chunkSize / 12; j++)
        {
            Solid.TextureHashes.Add(binaryReader.ReadUInt32());
            binaryReader.BaseStream.Position += 8;
        }
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
        Solid.Transform = header.Transform;
    }

    protected override void ProcessPlatChunk(BinaryReader binaryReader, uint chunkId, uint chunkSize)
    {
        switch (chunkId)
        {
            case 0x134900:
                ReadSolidPlatInfo(binaryReader);
                break;
            case 0x134901:
                ReadSolidPlatVertexBuffer(binaryReader, chunkSize);
                break;
            case 0x134902:
                ReadSolidPlatMeshEntries(binaryReader, chunkSize);
                break;
            case 0x134903:
                ReadSolidPlatIndices(binaryReader);
                break;
            case 0x134F01:
                ReadSolidPlatMaterialNameHashes(binaryReader);
                break;
            case 0x134C02:
                ReadSolidPlatMeshEntryName(binaryReader, chunkSize);
                break;
            case 0x134022: // bone hash list
                break;
            // case 0x134C04:
            //     ReadUCapFrameWeights(binaryReader, chunkSize);
            //     break;
            default:
                throw new InvalidDataException(
                    $"Not sure how to handle mesh chunk: 0x{chunkId:X} at 0x{binaryReader.BaseStream.Position:X}");
        }
    }

    private void ReadSolidPlatIndices(BinaryReader binaryReader)
    {
        BinaryUtil.AlignReader(binaryReader, 0x20);
        foreach (var material in Solid.Materials.OfType<UndercoverMaterial>())
        {
            material.Indices = new ushort[material.NumIndices];
            for (var j = 0; j < material.NumIndices; j++) material.Indices[j] = binaryReader.ReadUInt16();

            binaryReader.BaseStream.Position += 2 * material.NumReducedIndices;
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

    private void ReadSolidPlatMeshEntryName(BinaryReader binaryReader, uint chunkSize)
    {
        if (chunkSize > 0) Solid.Materials[_namedMaterials++].Name = BinaryUtil.ReadNullTerminatedString(binaryReader);
    }

    private void ReadSolidPlatMeshEntries(BinaryReader binaryReader, uint chunkSize)
    {
        chunkSize -= BinaryUtil.AlignReader(binaryReader, 0x80);
        Debug.Assert(chunkSize % 256 == 0);
        var numMats = chunkSize / 256;

        for (var j = 0; j < numMats; j++)
        {
            var shadingGroup = BinaryUtil.ReadStruct<Material>(binaryReader);
            var singleVertexSize = shadingGroup.VertexStream.VertexBytesAndFlag & 0x7FFF;

            Debug.Assert(shadingGroup.VertexStream.SizeOfVertexData == 0 ||
                         shadingGroup.VertexStream.SizeOfVertexData % singleVertexSize == 0);
            Debug.Assert(shadingGroup.TextureNumber[0] < Solid.TextureHashes.Count);

            var numVertices = shadingGroup.VertexStream.SizeOfVertexData == 0
                ? 0
                : shadingGroup.VertexStream.SizeOfVertexData / singleVertexSize;
            var solidObjectMaterial = new UndercoverMaterial
            {
                Flags = shadingGroup.MeshFlags,
                NumIndices = (uint)shadingGroup.IdxUsed,
                NumVerts = (uint)numVertices,
                VertexSetIndex = j,
                TextureHash = Solid.TextureHashes[shadingGroup.TextureNumber[0]],
                EffectId = EffectIdMapping[shadingGroup.MaterialAttribKey],
                NumReducedIndices = shadingGroup.NumReducedIdx,
                Indices = new ushort[shadingGroup.IdxUsed]
            };

            Solid.Materials.Add(solidObjectMaterial);
        }
    }

    private void ReadSolidPlatMaterialNameHashes(BinaryReader binaryReader)
    {
        BinaryUtil.AlignReader(binaryReader, 0x80);
        // TODO: do something with this data
        var numHashes = binaryReader.ReadUInt32();
        for (var i = 0; i < numHashes; i++) binaryReader.ReadUInt32();
    }

    private void ReadSolidPlatInfo(BinaryReader binaryReader)
    {
        BinaryUtil.AlignReader(binaryReader, 0x10);

        var descriptor = BinaryUtil.ReadUnmanagedStruct<SolidObjectDescriptor>(binaryReader);
        NumVertices = descriptor.NumVerts;
    }

    protected override SolidMeshVertex GetVertex(BinaryReader reader, UndercoverMaterial material, int stride)
    {
        var effectId = material.EffectId;
        var vertex = new SolidMeshVertex();

        switch (effectId)
        {
            case UndercoverEffectId.mw2_constant:
            case UndercoverEffectId.mw2_constant_alpha_bias:
            case UndercoverEffectId.mw2_illuminated:
            case UndercoverEffectId.mw2_pano:
                vertex.Position = BinaryUtil.ReadVector3(reader);
                reader.ReadSingle();
                vertex.TexCoords = BinaryUtil.ReadShort2N(reader) * 32;
                break;
            case UndercoverEffectId.mw2_matte:
            case UndercoverEffectId.mw2_diffuse_spec:
            case UndercoverEffectId.mw2_branches:
            case UndercoverEffectId.mw2_glass_no_n:
            case UndercoverEffectId.mw2_diffuse_spec_illum:
            case UndercoverEffectId.diffuse_spec_2sided:
            case UndercoverEffectId.mw2_combo_refl:
            case UndercoverEffectId.mw2_dirt:
            case UndercoverEffectId.mw2_grass:
            case UndercoverEffectId.mw2_tunnel_illum:
            case UndercoverEffectId.mw2_diffuse_spec_alpha:
            case UndercoverEffectId.mw2_trunk:
                vertex.Position = BinaryUtil.ReadVector3(reader);
                reader.ReadSingle();
                vertex.Color = reader.ReadUInt32();
                vertex.TexCoords = BinaryUtil.ReadShort2N(reader) * 32;
                vertex.Normal = BinaryUtil.ReadNormal(reader, true);
                break;
            case UndercoverEffectId.mw2_normalmap:
            case UndercoverEffectId.mw2_normalmap_bias:
            case UndercoverEffectId.mw2_glass_refl:
            case UndercoverEffectId.normalmap2sided:
            case UndercoverEffectId.mw2_road_overlay:
            case UndercoverEffectId.mw2_road_refl_overlay:
            case UndercoverEffectId.mw2_rock:
                vertex.Position = BinaryUtil.ReadVector3(reader);
                reader.ReadSingle();
                vertex.Color = reader.ReadUInt32();
                vertex.TexCoords = BinaryUtil.ReadShort2N(reader) * 32;
                vertex.Normal = BinaryUtil.ReadNormal(reader, true);
                // todo: read packed tangent vector
                reader.BaseStream.Position += 0x8;
                break;
            case UndercoverEffectId.mw2_grass_rock:
            case UndercoverEffectId.mw2_road_refl:
            case UndercoverEffectId.mw2_road_refl_tile:
            case UndercoverEffectId.mw2_road_tile:
            case UndercoverEffectId.mw2_dirt_rock:
                vertex.Position = BinaryUtil.ReadVector3(reader);
                reader.ReadSingle();
                vertex.Color = reader.ReadUInt32();
                vertex.TexCoords = BinaryUtil.ReadShort2N(reader) * 32;
                // TODO: TEXCOORD1??? what do we do with this?
                reader.ReadInt16();
                reader.ReadInt16();
                vertex.Normal = BinaryUtil.ReadNormal(reader, true);
                // todo: read packed tangent vector
                reader.BaseStream.Position += 0x8;
                break;
            case UndercoverEffectId.mw2_tunnel_road:
                vertex.Position = BinaryUtil.ReadVector3(reader);
                reader.ReadSingle();
                vertex.Color = reader.ReadUInt32();
                vertex.TexCoords = BinaryUtil.ReadShort2N(reader) * 32;
                // TODO: TEXCOORD1??? what do we do with this?
                reader.ReadInt16();
                reader.ReadInt16();
                // TODO: TEXCOORD2??? what do we do with this?
                reader.ReadInt16();
                reader.ReadInt16();
                vertex.Normal = BinaryUtil.ReadNormal(reader, true);
                // todo: read packed tangent vector
                reader.BaseStream.Position += 0x8;
                break;
            case UndercoverEffectId.mw2_road_refl_lite:
            case UndercoverEffectId.mw2_road_lite:
            case UndercoverEffectId.mw2_grass_dirt:
                vertex.Position = BinaryUtil.ReadVector3(reader);
                reader.ReadSingle();
                vertex.Color = reader.ReadUInt32();
                vertex.TexCoords = BinaryUtil.ReadShort2N(reader) * 32;
                // TODO: TEXCOORD1??? what do we do with this?
                reader.ReadInt16();
                reader.ReadInt16();
                vertex.Normal = BinaryUtil.ReadNormal(reader, true);
                break;
            case UndercoverEffectId.mw2_tunnel_wall:
                vertex.Position = BinaryUtil.ReadVector3(reader);
                reader.ReadSingle();
                vertex.Color = reader.ReadUInt32();
                vertex.TexCoords = BinaryUtil.ReadShort2N(reader) * 32;
                // TODO: TEXCOORD1??? what do we do with this?
                reader.ReadInt16();
                reader.ReadInt16();
                break;
            case UndercoverEffectId.mw2_matte_alpha:
            case UndercoverEffectId.mw2_dif_spec_a_bias:
            case UndercoverEffectId.mw2_dirt_overlay:
                vertex.Position = BinaryUtil.ReadVector3(reader);
                reader.ReadSingle();
                vertex.Color = reader.ReadUInt32();
                vertex.TexCoords = BinaryUtil.ReadShort2N(reader) * 32;
                vertex.Normal = BinaryUtil.ReadNormal(reader, true);
                break;
            case UndercoverEffectId.mw2_foliage:
            case UndercoverEffectId.mw2_foliage_lod:
            case UndercoverEffectId.mw2_scrub:
            case UndercoverEffectId.mw2_scrub_lod:
            case UndercoverEffectId.mw2_ocean:
                vertex.Position = BinaryUtil.ReadVector3(reader);
                reader.ReadSingle();
                vertex.Color = reader.ReadUInt32();
                vertex.TexCoords = BinaryUtil.ReadShort2N(reader) * 32;
                break;
            case UndercoverEffectId.mw2_sky:
                vertex.Position = BinaryUtil.ReadVector3(reader);
                reader.ReadSingle();
                vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                break;
            case UndercoverEffectId.mw2_texture_scroll:
            case UndercoverEffectId.mw2_car_heaven_default:
                vertex.Position = BinaryUtil.ReadVector3(reader);
                reader.ReadSingle();
                // TODO: COLOR0 is a float4. how do we deal with that?
                reader.BaseStream.Position += 0x10;
                vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                break;
            case UndercoverEffectId.mw2_car_heaven:
                vertex.Position = BinaryUtil.ReadVector3(reader);
                reader.ReadSingle();
                // TODO: COLOR0 is a float4. how do we deal with that?
                reader.BaseStream.Position += 0x10;
                vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                vertex.Normal = BinaryUtil.ReadNormal(reader, true);
                break;
            case UndercoverEffectId.mw2_carhvn_floor:
            case UndercoverEffectId.mw2_road:
                vertex.Position = BinaryUtil.ReadVector3(reader);
                reader.ReadSingle();
                vertex.Color = reader.ReadUInt32();
                vertex.TexCoords = BinaryUtil.ReadShort2N(reader) * 32;
                // TODO: TEXCOORD1??? what do we do with this?
                reader.ReadInt16();
                reader.ReadInt16();
                vertex.Normal = BinaryUtil.ReadNormal(reader, true);
                // todo: read packed tangent vector
                reader.BaseStream.Position += 0x8;
                break;
            case UndercoverEffectId.car:
            case UndercoverEffectId.car_a:
            case UndercoverEffectId.car_a_nzw:
            case UndercoverEffectId.car_nm:
            case UndercoverEffectId.car_nm_a:
            case UndercoverEffectId.car_nm_v_s:
            case UndercoverEffectId.car_nm_v_s_a:
            case UndercoverEffectId.car_si:
            case UndercoverEffectId.car_si_a:
            case UndercoverEffectId.car_t:
            case UndercoverEffectId.car_t_a:
            case UndercoverEffectId.car_t_nm:
            case UndercoverEffectId.car_v:
                vertex.Position = BinaryUtil.ReadNormal(reader, true) * 10;
                vertex.TexCoords = BinaryUtil.ReadShort2N(reader) * 32;
                vertex.Color = reader.ReadUInt32();
                vertex.Normal = BinaryUtil.ReadNormal(reader, true);
                vertex.Tangent = BinaryUtil.ReadNormal(reader, true);
                break;
            case UndercoverEffectId.mw2_indicator:
                vertex.Position = BinaryUtil.ReadVector3(reader);
                reader.ReadSingle();
                vertex.Color = reader.ReadUInt32();
                vertex.TexCoords = BinaryUtil.ReadShort2N(reader) * 32;
                break;
            case UndercoverEffectId.mw2_smokegeo:
                vertex.Position = BinaryUtil.ReadVector3(reader);
                vertex.TexCoords = BinaryUtil.ReadShort2N(reader) * 32;
                vertex.Normal = BinaryUtil.ReadNormal(reader, true);
                break;
            case UndercoverEffectId.mw2_cardebris:
                vertex.Position = BinaryUtil.ReadVector3(reader);
                vertex.Normal = BinaryUtil.ReadNormal(reader, true);
                break;
            case UndercoverEffectId.watersplash:
                vertex.Position = BinaryUtil.ReadVector3(reader);
                reader.ReadSingle();
                vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                vertex.Normal = BinaryUtil.ReadVector3(reader);
                reader.ReadSingle();
                vertex.Tangent = BinaryUtil.ReadVector3(reader);
                reader.ReadSingle();
                break;
            case UndercoverEffectId.worldbone:
            case UndercoverEffectId.worldbonenocull:
            case UndercoverEffectId.worldbonetransparency:
                vertex.Position = BinaryUtil.ReadVector3(reader);
                reader.ReadSingle();
                vertex.TexCoords = BinaryUtil.ReadShort2N(reader) * 32;
                reader.BaseStream.Position += 8; // BLEND{WEIGHT,INDICES}0
                vertex.Normal = BinaryUtil.ReadNormal(reader, true);
                vertex.Tangent = BinaryUtil.ReadNormal(reader, true);
                break;
            default:
                throw new Exception($"Unsupported effect: {effectId}");
        }

        return vertex;
    }

    protected override void ProcessVertices(ref SolidMeshVertex[] vertices, int vertexStreamId)
    {
        //
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct SolidObjectHeader
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        private readonly byte[] zero;

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

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
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

        public readonly ulong Padding, Padding2;

        public uint Padding3;

        public readonly uint NumTris;
        public readonly uint NumTriIdx;
        public readonly uint NumReducedTris;
        public readonly uint NumReducedTriIdx;

        public readonly ushort NumVerts;
        public readonly ushort Unknown3;

        public readonly uint Zero3;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 40)]
    internal struct eVertexStream
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public uint[] VertexBufferOpaque; // @0x0

        public uint SizeOfVertexData; // @0x20
        public byte Stream; // @0x24
        public byte LoadStreamIndex; // @0x25

        public short VertexBytesAndFlag; // @0x26
        // end: 0x28
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct Material
    {
        public readonly int FirstIndex; // @0x0
        public readonly int LastIndex; // @0x4

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public readonly int[] TextureParam; // @0x8

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public readonly byte[] TextureNumber; // @0x30

        public readonly byte NumTextures; // @0x3A
        public readonly byte LightMaterialNumber; // @0x3B

        public readonly uint TextureSortKey; // @0x3C
        public readonly uint MeshFlags; // @0x40
        public readonly int IdxUsed; // @0x44

        public readonly uint NumReducedIdx;
        public eVertexStream VertexStream; // @0x48

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public readonly int[] Zero4; // @0x74

        public uint MaterialAttribKey; // @0x84
        public uint MaterialAttribPointer; // @0x88

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public readonly int[] TextureParamMaterial; // @0x8C

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public readonly uint[] TextureNameMaterial; // @0xB4

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 36)]
        public readonly byte[] Zero5; // @0xDC

        // end: 0x100
    }
}