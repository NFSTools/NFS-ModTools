using System;
using System.Collections.Generic;
using System.IO;

namespace Common.Geometry.Data
{
    public class UndercoverObject : SolidObject
    {
        public enum EffectID
        {
            car,
            car_a,
            car_a_nzw,
            car_nm,
            car_nm_a,
            car_nm_v_s,
            car_nm_v_s_a,
            car_si,
            car_si_a,
            car_t,
            car_t_a,
            car_t_nm,
            car_v,
            diffuse_spec_2sided,
            mw2_branches,
            mw2_car_heaven,
            mw2_car_heaven_default,
            mw2_cardebris,
            mw2_carhvn_floor,
            mw2_combo_refl,
            mw2_constant,
            mw2_constant_alpha_bias,
            mw2_dif_spec_a_bias,
            mw2_diffuse_spec,
            mw2_diffuse_spec_alpha,
            mw2_diffuse_spec_illum,
            mw2_diffuse_spec_salpha,
            mw2_dirt,
            mw2_dirt_overlay,
            mw2_dirt_rock,
            mw2_fol_alwaysfacing,
            mw2_foliage,
            mw2_foliage_lod,
            mw2_glass_no_n,
            mw2_glass_refl,
            mw2_grass,
            mw2_grass_dirt,
            mw2_grass_rock,
            mw2_icon,
            mw2_illuminated,
            mw2_indicator,
            mw2_matte,
            mw2_matte_alpha,
            mw2_normalmap,
            mw2_normalmap_bias,
            mw2_ocean,
            mw2_pano,
            mw2_parallax,
            mw2_road,
            mw2_road_lite,
            mw2_road_overlay,
            mw2_road_refl,
            mw2_road_refl_lite,
            mw2_road_refl_overlay,
            mw2_road_refl_tile,
            mw2_road_tile,
            mw2_rock,
            mw2_rock_overlay,
            mw2_scrub,
            mw2_scrub_lod,
            mw2_sky,
            mw2_smokegeo,
            mw2_texture_scroll,
            mw2_trunk,
            mw2_tunnel_illum,
            mw2_tunnel_road,
            mw2_tunnel_wall,
            normalmap2sided,
            shadowmesh,
            standardeffect,
            ubereffect,
            ubereffectblend,
            watersplash,
            worldbone,
            worldbonenocull,
            worldbonetransparency,
        }

        public List<string> TextureTypeList { get; set; }

        public UndercoverObject()
        {
            TextureTypeList = new List<string>();
        }

        /// <inheritdoc />
        /// <summary>
        /// This thing is responsible for somehow deriving proper vertex data from
        /// a NFS:Undercover vertex buffer. Getting the coordinates is easy, but
        /// the hard part is getting the texture coordinates. Can it be done? Who knnows?
        /// </summary>
        /// <remarks>And I didn't even mention the fact that there are wayyyy too many edge cases...</remarks>
        /// <remarks>I hate this game. Worse than E.T.</remarks>
        /// <param name="reader"></param>
        /// <param name="material"></param>
        /// <param name="stride"></param>
        /// <returns></returns>
        protected override SolidMeshVertex GetVertex(BinaryReader reader, SolidObjectMaterial material, int stride)
        {
            var effectId = ((UndercoverMaterial)material).EffectId;
            SolidMeshVertex vertex = new SolidMeshVertex();

            switch (effectId)
            {
                case EffectID.mw2_constant:
                case EffectID.mw2_constant_alpha_bias:
                case EffectID.mw2_illuminated:
                case EffectID.mw2_pano:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    reader.ReadSingle();
                    vertex.TexCoords = BinaryUtil.ReadUV(reader, true);
                    break;
                case EffectID.mw2_matte:
                case EffectID.mw2_diffuse_spec:
                case EffectID.mw2_branches:
                case EffectID.mw2_glass_no_n:
                case EffectID.mw2_diffuse_spec_illum:
                case EffectID.diffuse_spec_2sided:
                case EffectID.mw2_combo_refl:
                case EffectID.mw2_dirt:
                case EffectID.mw2_grass:
                case EffectID.mw2_tunnel_illum:
                case EffectID.mw2_diffuse_spec_alpha:
                case EffectID.mw2_trunk:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    reader.ReadSingle();
                    vertex.Color = reader.ReadUInt32();
                    vertex.TexCoords = BinaryUtil.ReadUV(reader, true);
                    vertex.Normal = BinaryUtil.ReadNormal(reader, true);
                    break;
                case EffectID.mw2_normalmap:
                case EffectID.mw2_normalmap_bias:
                case EffectID.mw2_glass_refl:
                case EffectID.normalmap2sided:
                case EffectID.mw2_road_overlay:
                case EffectID.mw2_road_refl_overlay:
                case EffectID.mw2_rock:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    reader.ReadSingle();
                    vertex.Color = reader.ReadUInt32();
                    vertex.TexCoords = BinaryUtil.ReadUV(reader, true);
                    vertex.Normal = BinaryUtil.ReadNormal(reader, true);
                    // todo: read packed tangent vector
                    reader.BaseStream.Position += 0x8;
                    break;
                case EffectID.mw2_grass_rock:
                case EffectID.mw2_road_refl:
                case EffectID.mw2_road_refl_tile:
                case EffectID.mw2_road_tile:
                case EffectID.mw2_dirt_rock:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    reader.ReadSingle();
                    vertex.Color = reader.ReadUInt32();
                    vertex.TexCoords = BinaryUtil.ReadUV(reader, true);
                    // TODO: TEXCOORD1??? what do we do with this?
                    reader.ReadInt16();
                    reader.ReadInt16();
                    vertex.Normal = BinaryUtil.ReadNormal(reader, true);
                    // todo: read packed tangent vector
                    reader.BaseStream.Position += 0x8;
                    break;
                case EffectID.mw2_tunnel_road:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    reader.ReadSingle();
                    vertex.Color = reader.ReadUInt32();
                    vertex.TexCoords = BinaryUtil.ReadUV(reader, true);
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
                case EffectID.mw2_road_refl_lite:
                case EffectID.mw2_road_lite:
                case EffectID.mw2_grass_dirt:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    reader.ReadSingle();
                    vertex.Color = reader.ReadUInt32();
                    vertex.TexCoords = BinaryUtil.ReadUV(reader, true);
                    // TODO: TEXCOORD1??? what do we do with this?
                    reader.ReadInt16();
                    reader.ReadInt16();
                    vertex.Normal = BinaryUtil.ReadNormal(reader, true);
                    break;
                case EffectID.mw2_tunnel_wall:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    reader.ReadSingle();
                    vertex.Color = reader.ReadUInt32();
                    vertex.TexCoords = BinaryUtil.ReadUV(reader, true);
                    // TODO: TEXCOORD1??? what do we do with this?
                    reader.ReadInt16();
                    reader.ReadInt16();
                    break;
                case EffectID.mw2_matte_alpha:
                case EffectID.mw2_dif_spec_a_bias:
                case EffectID.mw2_dirt_overlay:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    reader.ReadSingle();
                    vertex.Color = reader.ReadUInt32();
                    vertex.TexCoords = BinaryUtil.ReadUV(reader, true);
                    vertex.Normal = BinaryUtil.ReadNormal(reader, true);
                    break;
                case EffectID.mw2_foliage:
                case EffectID.mw2_foliage_lod:
                case EffectID.mw2_scrub:
                case EffectID.mw2_scrub_lod:
                case EffectID.mw2_ocean:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    reader.ReadSingle();
                    vertex.Color = reader.ReadUInt32();
                    vertex.TexCoords = BinaryUtil.ReadUV(reader, true);
                    break;
                case EffectID.mw2_sky:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    reader.ReadSingle();
                    vertex.TexCoords = BinaryUtil.ReadUV(reader);
                    break;
                case EffectID.mw2_texture_scroll:
                case EffectID.mw2_car_heaven_default:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    reader.ReadSingle();
                    // TODO: COLOR0 is a float4. how do we deal with that?
                    reader.BaseStream.Position += 0x10;
                    vertex.TexCoords = BinaryUtil.ReadUV(reader);
                    break;
                case EffectID.mw2_car_heaven:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    reader.ReadSingle();
                    // TODO: COLOR0 is a float4. how do we deal with that?
                    reader.BaseStream.Position += 0x10;
                    vertex.TexCoords = BinaryUtil.ReadUV(reader);
                    vertex.Normal = BinaryUtil.ReadNormal(reader, true);
                    break;
                case EffectID.mw2_carhvn_floor:
                case EffectID.mw2_road:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    reader.ReadSingle();
                    vertex.Color = reader.ReadUInt32();
                    vertex.TexCoords = BinaryUtil.ReadUV(reader, true);
                    // TODO: TEXCOORD1??? what do we do with this?
                    reader.ReadInt16();
                    reader.ReadInt16();
                    vertex.Normal = BinaryUtil.ReadNormal(reader, true);
                    // todo: read packed tangent vector
                    reader.BaseStream.Position += 0x8;
                    break;
                case EffectID.car:
                case EffectID.car_a:
                case EffectID.car_a_nzw:
                case EffectID.car_nm:
                case EffectID.car_nm_a:
                case EffectID.car_nm_v_s:
                case EffectID.car_nm_v_s_a:
                case EffectID.car_si:
                case EffectID.car_si_a:
                case EffectID.car_t:
                case EffectID.car_t_a:
                case EffectID.car_t_nm:
                case EffectID.car_v:
                    vertex.Position = BinaryUtil.ReadNormal(reader, true) * 10;
                    vertex.TexCoords = BinaryUtil.ReadUV(reader, true);
                    vertex.Color = reader.ReadUInt32();
                    vertex.Normal = BinaryUtil.ReadNormal(reader, true);
                    vertex.Tangent = BinaryUtil.ReadNormal(reader, true);
                    break;
                default:
                    throw new Exception($"Unsupported effect: {effectId}");
            }

            return vertex;
        }
    }
}