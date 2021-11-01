using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Common.Geometry.Data
{
    public class SolidObjectMaterial
    {
        public Vector3 MinPoint { get; set; }

        public Vector3 MaxPoint { get; set; }

        public uint Hash { get; set; }

        public uint Flags { get; set; }

        public uint NumVerts { get; set; }

        public int VertexStreamIndex { get; set; }

        public uint NumIndices { get; set; } // NumTris * 3

        public uint TextureHash { get; set; }

        public string Name { get; set; }
        public ushort[] Indices { get; set; }
        public SolidMeshVertex[] Vertices { get; set; }
    }

    public class MostWantedMaterial : SolidObjectMaterial
    {
        public uint EffectID { get; set; }
    }

    public class Underground2Material : SolidObjectMaterial
    {
    }

    public class CarbonMaterial : SolidObjectMaterial
    {
        public int EffectID { get; set; }
    }

    public class UndercoverMaterial : CarbonMaterial
    {
        public UndercoverObject.EffectID EffectId { get; set; }
        public uint NumReducedIndices { get; set; }
    }

    public class ProStreetMaterial : CarbonMaterial
    {
        public ProStreetObject.EffectID EffectId { get; set; }
    }

    public class World15Material : CarbonMaterial
    {
    }

    public struct SolidMeshVertex
    {
        // REQUIRED attributes
        public Vector3 Position { get; set; }
        public Vector2 TexCoords { get; set; }

        // OPTIONAL attributes
        public Vector3? Normal { get; set; }
        public Vector3? Tangent { get; set; }

        public uint? Color { get; set; }
    }

    public class SolidMeshDescriptor
    {
        public uint Flags { get; set; }

        public uint NumMats { get; set; }

        public uint NumVertexStreams { get; set; }

        public uint NumIndices { get; set; } // NumTris * 3
        public uint NumTris { get; set; }

        // dynamically computed
        public bool HasNormals { get; set; }

        // dynamically computed
        public uint NumVerts { get; set; }
    }

    public class UndergroundObject : SolidObject
    {
        protected override SolidMeshVertex GetVertex(BinaryReader reader, SolidObjectMaterial material, int stride)
        {
            SolidMeshVertex vertex;
            switch (stride)
            {
                // position (12 bytes) + color (4 bytes) + tex coords (8 bytes)
                case 24:
                    vertex = new SolidMeshVertex
                    {
                        Position = BinaryUtil.ReadVector3(reader),
                        Color = reader.ReadUInt32(),
                        TexCoords = BinaryUtil.ReadUV(reader)
                    };
                    break;
                // position (12 bytes) + normal (12 bytes) + color (4 bytes) + tex coords (8 bytes)
                case 36:
                    vertex = new SolidMeshVertex
                    {
                        Position = BinaryUtil.ReadVector3(reader),
                        Normal = BinaryUtil.ReadVector3(reader),
                        Color = reader.ReadUInt32(),
                        TexCoords = BinaryUtil.ReadUV(reader)
                    };
                    break;
                // position (12 bytes) + color (4 bytes) + normal (12 bytes) + tex coords (8 bytes)
                case 60:
                    vertex = new SolidMeshVertex
                    {
                        Position = BinaryUtil.ReadVector3(reader),
                        Normal = BinaryUtil.ReadVector3(reader),
                        Color = reader.ReadUInt32(),
                        TexCoords = BinaryUtil.ReadUV(reader)
                    };
                    reader.BaseStream.Position += 24;
                    break;
                default:
                    throw new Exception($"Cannot handle vertex size: {stride}");
            }

            return vertex;
        }
    }

    public class Underground2Object : SolidObject
    {
        protected override SolidMeshVertex GetVertex(BinaryReader reader, SolidObjectMaterial material, int stride)
        {
            SolidMeshVertex vertex;
            switch (stride)
            {
                // position (12 bytes) + normal (12 bytes) + color (4 bytes) + tex coords (8 bytes)
                case 36:
                    vertex = new SolidMeshVertex
                    {
                        Position = BinaryUtil.ReadVector3(reader),
                        Normal = BinaryUtil.ReadVector3(reader),
                        Color = reader.ReadUInt32(),
                        TexCoords = BinaryUtil.ReadUV(reader)
                    };
                    break;
                // position (12 bytes) + color (4 bytes) + tex coords (8 bytes)
                case 24:
                    vertex = new SolidMeshVertex
                    {
                        Position = BinaryUtil.ReadVector3(reader),
                        Color = reader.ReadUInt32(),
                        TexCoords = BinaryUtil.ReadUV(reader)
                    };
                    break;
                default:
                    throw new Exception($"Cannot handle vertex size: {stride}");
            }

            return vertex;
        }
    }

    public class MostWantedObject : SolidObject
    {
        enum InternalEffectID
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
            NormalMapNoFog,
        }

        protected override SolidMeshVertex GetVertex(BinaryReader reader, SolidObjectMaterial material, int stride)
        {
            MostWantedMaterial mwm = (MostWantedMaterial) material;
            SolidMeshVertex vertex = new SolidMeshVertex();

            InternalEffectID id = (InternalEffectID) mwm.EffectID;

            switch (id)
            {
                case InternalEffectID.WorldNormalMap:
                case InternalEffectID.WorldReflectShader:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    vertex.Normal = BinaryUtil.ReadVector3(reader);
                    vertex.Color = reader.ReadUInt32();
                    vertex.TexCoords = BinaryUtil.ReadUV(reader);
                    reader.BaseStream.Position += 8; // TODO: What's this additional D3DDECLUSAGE_TEXCOORD element?
                    vertex.Tangent = BinaryUtil.ReadVector3(reader);
                    reader.BaseStream.Position += 4; // skip W component of tangent vector
                    break;
                case InternalEffectID.skyshader:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    vertex.Normal = BinaryUtil.ReadVector3(reader);
                    vertex.Color = reader.ReadUInt32();
                    vertex.TexCoords = BinaryUtil.ReadUV(reader);
                    reader.BaseStream.Position += 8; // TODO: What's this additional D3DDECLUSAGE_TEXCOORD element?
                    break;
                case InternalEffectID.WorldShader:
                case InternalEffectID.GlossyWindow:
                case InternalEffectID.billboardshader:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    vertex.Normal = BinaryUtil.ReadVector3(reader);
                    vertex.Color = reader.ReadUInt32();
                    vertex.TexCoords = BinaryUtil.ReadUV(reader);
                    break;
                default:
                    throw new Exception($"Unsupported effect in object {Name}: {id}");
            }

            return vertex;
        }
    }

    public class CarbonObject : SolidObject
    {
        enum InternalEffectID
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
            GHOSTCAR,
        }

        protected override SolidMeshVertex GetVertex(BinaryReader reader, SolidObjectMaterial material, int stride)
        {
            CarbonMaterial cm = (CarbonMaterial)material;
            SolidMeshVertex vertex = new SolidMeshVertex();

            InternalEffectID id = (InternalEffectID)cm.EffectID;

            switch (id)
            {
                case InternalEffectID.WorldShader:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    vertex.Normal = BinaryUtil.ReadVector3(reader);
                    vertex.Color = reader.ReadUInt32();
                    vertex.TexCoords = BinaryUtil.ReadUV(reader);
                    break;
                case InternalEffectID.skyshader:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    vertex.Normal = BinaryUtil.ReadVector3(reader);
                    vertex.Color = reader.ReadUInt32();
                    vertex.TexCoords = BinaryUtil.ReadUV(reader);
                    reader.BaseStream.Position += 8; // TODO: What's this additional D3DDECLUSAGE_TEXCOORD element?
                    break;
                case InternalEffectID.WorldNormalMap:
                case InternalEffectID.WorldReflectShader:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    vertex.Normal = BinaryUtil.ReadVector3(reader);
                    vertex.Color = reader.ReadUInt32();
                    vertex.TexCoords = BinaryUtil.ReadUV(reader);
                    vertex.Tangent = BinaryUtil.ReadVector3(reader);
                    reader.BaseStream.Position += 4; // skip W component of tangent vector
                    break;
                case InternalEffectID.CarShader:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    vertex.Normal = BinaryUtil.ReadVector3(reader);
                    vertex.Color = reader.ReadUInt32();
                    vertex.TexCoords = BinaryUtil.ReadUV(reader);
                    vertex.Tangent = BinaryUtil.ReadVector3(reader);
                    break;
                case InternalEffectID.CARNORMALMAP:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    vertex.TexCoords = BinaryUtil.ReadUV(reader);
                    vertex.Color = reader.ReadUInt32();
                    vertex.Normal = BinaryUtil.ReadVector3(reader);
                    vertex.Tangent = BinaryUtil.ReadVector3(reader);
                    break;
                case InternalEffectID.GLASS_REFLECT:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    vertex.TexCoords = BinaryUtil.ReadUV(reader);
                    vertex.Color = reader.ReadUInt32();
                    vertex.Normal = BinaryUtil.ReadVector3(reader);
                    break;
                case InternalEffectID.WATER:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    vertex.Color = reader.ReadUInt32();
                    vertex.TexCoords = BinaryUtil.ReadUV(reader);
                    vertex.Normal = BinaryUtil.ReadVector3(reader);
                    break;
                default:
                    throw new Exception($"Unsupported effect in object {Name}: {id}");
            }

            return vertex;
        }
    }

    public class ProStreetObject : SolidObject
    {
        public enum EffectID
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
            ALWAYSFACING,
        }

        protected override SolidMeshVertex GetVertex(BinaryReader reader, SolidObjectMaterial material, int stride)
        {
            ProStreetMaterial psm = (ProStreetMaterial)material;
            SolidMeshVertex vertex = new SolidMeshVertex();

            switch (psm.EffectId)
            {
                case EffectID.WORLDBAKEDLIGHTING:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    vertex.Color = reader.ReadUInt32();
                    vertex.TexCoords = BinaryUtil.ReadUV(reader);
                    reader.BaseStream.Position += 16;
                    vertex.Normal = BinaryUtil.ReadNormal(reader, true);
                    break;
                case EffectID.WORLD:
                case EffectID.SKY:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    vertex.Normal = BinaryUtil.ReadNormal(reader);
                    vertex.Color = reader.ReadUInt32();
                    vertex.TexCoords = BinaryUtil.ReadUV(reader);
                    reader.BaseStream.Position += 8; // TODO: what is this second D3DDECLUSAGE_TEXCOORD element?
                    break;
                case EffectID.WORLDNORMALMAP:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    vertex.Normal = BinaryUtil.ReadNormal(reader);
                    vertex.Color = reader.ReadUInt32();
                    vertex.TexCoords = BinaryUtil.ReadUV(reader);
                    reader.BaseStream.Position += 8; // TODO: what is this second D3DDECLUSAGE_TEXCOORD element?
                    // TODO: read tangent
                    reader.BaseStream.Position += 16;
                    reader.BaseStream.Position += 8; // TODO: what are these other D3DDECLUSAGE_TEXCOORD elements? (2x D3DDECLTYPE_UBYTE4)
                    break;
                case EffectID.WorldDepthShader:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    vertex.Normal = BinaryUtil.ReadNormal(reader);
                    vertex.Color = reader.ReadUInt32();
                    vertex.TexCoords = BinaryUtil.ReadUV(reader);
                    break;
                case EffectID.TREELEAVES:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    vertex.Color = reader.ReadUInt32();
                    vertex.Normal = BinaryUtil.ReadNormal(reader);
                    // TODO: COLLADA supports tangent vectors, so we should eventually read them
                    //> elements[3]: type=D3DDECLTYPE_FLOAT4 usage=D3DDECLUSAGE_TANGENT size=16 offset=0x1c
                    reader.BaseStream.Position += 0x10;
                    vertex.TexCoords = BinaryUtil.ReadUV(reader);
                    break;
                case EffectID.WORLDCONSTANT:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    vertex.TexCoords = BinaryUtil.ReadUV(reader);
                    vertex.Color = reader.ReadUInt32();
                    break;
                case EffectID.ROAD:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    vertex.TexCoords = BinaryUtil.ReadUV(reader);
                    reader.BaseStream.Position += 28;
                    vertex.Color = reader.ReadUInt32();
                    break;
                case EffectID.TERRAIN:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    vertex.TexCoords = BinaryUtil.ReadUV(reader);
                    reader.BaseStream.Position += 16;
                    vertex.Normal = BinaryUtil.ReadNormal(reader, true);
                    // TODO: read packed tangent vector (4 short-components)
                    reader.BaseStream.Position += 8;
                    vertex.Color = reader.ReadUInt32();
                    reader.BaseStream.Position += 8; // TODO: what are these other D3DDECLUSAGE_TEXCOORD elements? (2x D3DDECLTYPE_UBYTE4)
                    break;
                case EffectID.GRASSTERRAIN:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    vertex.TexCoords = BinaryUtil.ReadUV(reader);
                    reader.BaseStream.Position += 20;
                    vertex.Normal = BinaryUtil.ReadNormal(reader, true);
                    // TODO: read packed tangent vector (4 short-components)
                    reader.BaseStream.Position += 8;
                    vertex.Color = reader.ReadUInt32();
                    break;
                case EffectID.FLAG:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    vertex.Color = reader.ReadUInt32();
                    vertex.Normal = BinaryUtil.ReadNormal(reader, true);
                    vertex.TexCoords = BinaryUtil.ReadUV(reader);
                    reader.BaseStream.Position += 8; // TODO: what is this second D3DDECLUSAGE_TEXCOORD element?
                    break;
                case EffectID.GRASSCARD:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    vertex.Color = reader.ReadUInt32();
                    vertex.Normal = BinaryUtil.ReadNormal(reader, true);
                    // TODO: read packed tangent vector (4 short-components)
                    reader.BaseStream.Position += 8;
                    vertex.TexCoords = BinaryUtil.ReadUV(reader);
                    reader.BaseStream.Position += 8; // TODO: what is this second D3DDECLUSAGE_TEXCOORD element?
                    break;
                case EffectID.CAR:
                case EffectID.CARNORMALMAP:
                case EffectID.CARVINYL:
                    vertex.Position = BinaryUtil.ReadNormal(reader, true);
                    vertex.TexCoords = BinaryUtil.ReadUV(reader, true);
                    vertex.Color = reader.ReadUInt32();
                    vertex.Normal = BinaryUtil.ReadNormal(reader, true);
                    vertex.Tangent = BinaryUtil.ReadNormal(reader, true);
                    break;
                default:
                    throw new Exception($"Unsupported effect in object {Name}: {psm.EffectId}");
            }

            return vertex;
        }
    }

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

    public class World15Object : SolidObject
    {
        enum InternalEffectID
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
            WATER,
        }

        public struct MorphInfo
        {
            // morph range is [VertexStartIndex, VertexEndIndex]
            public int VertexStartIndex;
            public int VertexEndIndex;
            public int MorphMatrixIndex;
        }

        public List<List<MorphInfo>> MorphLists { get; set; }
        public List<Matrix4x4> MorphMatrices { get; set; }

        public World15Object()
        {
            MorphLists = new List<List<MorphInfo>>();
            MorphMatrices = new List<Matrix4x4>();
        }

        protected override SolidMeshVertex GetVertex(BinaryReader reader, SolidObjectMaterial material, int stride)
        {
            World15Material wm = (World15Material) material;
            SolidMeshVertex vertex = new SolidMeshVertex();

            InternalEffectID id = (InternalEffectID)wm.EffectID;

            switch (id)
            {
                case InternalEffectID.WorldShader:
                case InternalEffectID.GLASS_REFLECT:
                case InternalEffectID.WorldZBiasShader:
                case InternalEffectID.Tree:
                case InternalEffectID.WATER:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    vertex.Normal = BinaryUtil.ReadNormal(reader, true);
                    vertex.TexCoords = BinaryUtil.ReadUV(reader);
                    vertex.Color = reader.ReadUInt32(); // daytime color
                    reader.ReadUInt32(); // nighttime color
                    break;
                case InternalEffectID.WorldPrelitShader:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    vertex.Color = reader.ReadUInt32(); // daytime color
                    reader.ReadUInt32(); // nighttime color
                    vertex.TexCoords = BinaryUtil.ReadUV(reader);
                    break;
                case InternalEffectID.WorldZBiasPrelitShader:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    vertex.Normal = BinaryUtil.ReadNormal(reader, true);
                    vertex.Color = reader.ReadUInt32(); // daytime color
                    reader.ReadUInt32(); // nighttime color
                    vertex.TexCoords = BinaryUtil.ReadUV(reader);
                    break;
                case InternalEffectID.WorldNormalMap:
                case InternalEffectID.GLASS_REFLECTNM:
                case InternalEffectID.WorldRoadShader:
                case InternalEffectID.WorldFEShader:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    vertex.Normal = BinaryUtil.ReadNormal(reader, true);
                    vertex.TexCoords = BinaryUtil.ReadUV(reader);
                    vertex.Color = reader.ReadUInt32(); // daytime color
                    reader.ReadUInt32(); // nighttime color
                    vertex.Tangent = BinaryUtil.ReadNormal(reader, true);
                    break;
                case InternalEffectID.CarShader:
                case InternalEffectID.CARNORMALMAP:
                    vertex.Position = BinaryUtil.ReadNormal(reader, true) * 8;
                    vertex.TexCoords = new Vector2(reader.ReadInt16() / 4096f, 1 - reader.ReadInt16() / 4096f);
                    vertex.Color = reader.ReadUInt32();
                    vertex.Normal = BinaryUtil.ReadNormal(reader, true);
                    vertex.Tangent = BinaryUtil.ReadNormal(reader, true);
                    break;
                default:
                    throw new Exception($"Unsupported effect in object {Name}: {id}");
            }

            return vertex;
        }

        public override void PostProcessing()
        {
            Debug.Assert(this.MorphLists.Count == 0 || this.MorphLists.Count == this.VertexBuffers.Count);

            base.PostProcessing();
        }

        protected override void ProcessVertices(ref SolidMeshVertex?[] vertices, int streamIndex)
        {
            base.ProcessVertices(ref vertices, streamIndex);

            // If we don't have a morph list for the stream, bail out
            if (this.MorphLists.Count <= streamIndex) return;

            var morphList = this.MorphLists[streamIndex];

            foreach (var morphInfo in morphList)
            {
                for (var i = morphInfo.VertexStartIndex;
                    i <= morphInfo.VertexEndIndex;
                    i++)
                {
                    Debug.Assert(vertices[i].HasValue);
                    var vertex = vertices[i].Value;
                    vertex.Position = Vector3.Transform(
                        vertex.Position, 
                        this.MorphMatrices[morphInfo.MorphMatrixIndex]);
                    vertices[i] = vertex;
                }
            }
        }
    }

    public abstract class SolidObject : ChunkManager.BasicResource
    {
        private sealed class HashEqualityComparer : IEqualityComparer<SolidObject>
        {
            public bool Equals(SolidObject x, SolidObject y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return x.Hash == y.Hash;
            }

            public int GetHashCode(SolidObject obj)
            {
                return (int) obj.Hash;
            }
        }

        public static IEqualityComparer<SolidObject> HashComparer { get; } = new HashEqualityComparer();

        public string Name { get; set; }

        public Matrix4x4 Transform { get; set; }

        public Vector3 MinPoint { get; set; }

        public Vector3 MaxPoint { get; set; }

        public uint Hash { get; set; }

        public uint NumTextures { get; set; }

        public uint NumShaders { get; set; }

        public uint NumTris { get; set; }

        public SolidMeshDescriptor MeshDescriptor { get; set; }

        public List<SolidObjectMaterial> Materials { get; } = new List<SolidObjectMaterial>();

        public List<uint> TextureHashes { get; } = new List<uint>();

        public List<byte[]> VertexBuffers { get; } = new List<byte[]>();

        /// <summary>
        /// Pull a vertex from the given buffer.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="material"></param>
        /// <param name="stride"></param>
        /// <returns></returns>
        protected abstract SolidMeshVertex GetVertex(BinaryReader reader, SolidObjectMaterial material, int stride);

        /// <summary>
        /// Do any necessary post-processing of an entire vertex stream.
        /// </summary>
        /// <param name="vertices">The array of vertices</param>
        /// <param name="streamIndex">The index of the vertex stream</param>
        protected virtual void ProcessVertices(ref SolidMeshVertex?[] vertices, int streamIndex)
        {
            //
        }

        /// <summary>
        /// Process the model data.
        /// </summary>
        public virtual void PostProcessing()
        {
            // Each vertex buffer gets its own BinaryReader.
            var vbStreams = new BinaryReader[VertexBuffers.Count];
            // Bookkeeping array: how many vertices are in a given buffer?
            var vbCounts = new uint[VertexBuffers.Count];
            // Bookkeeping array: how many vertices have been consumed from a given buffer?
            var vbOffsets = new uint[VertexBuffers.Count];
            // Each vertex buffer also gets its own array of vertex objects
            var vbArrays = new SolidMeshVertex?[VertexBuffers.Count][];

            for (var i = 0; i < vbStreams.Length; i++)
            {
                vbStreams[i] = new BinaryReader(new MemoryStream(VertexBuffers[i]));
            }

            // Filling in vbCounts...
            if (VertexBuffers.Count == 1)
            {
                // The mesh descriptor tells us how many vertices exist.
                // Since we only have one vertex buffer, we can use the info
                // from the mesh descriptor instead of doing the material loop
                // seen after this block.
                vbCounts[0] = MeshDescriptor.NumVerts;
            }
            else if (VertexBuffers.Count > 1)
            {
                // Fill in vbCounts by examining every material.
                // We need to make sure at least one of our materials
                // actually has a NumVerts > 0, otherwise weird things
                // will probably happen.
                Debug.Assert(Materials.Any(m => m.NumVerts > 0));
                foreach (var t in Materials)
                {
                    vbCounts[t.VertexStreamIndex] += t.NumVerts;
                }
            }
            else
            {
                // If we have no vertex buffers, we can bail out.
                return;
            }

            // Verifying the integrity of our data...
            for (var i = 0; i < vbStreams.Length; i++)
            {
                // To avoid a division-by-zero exception, we allow a vertex buffer to have
                // EITHER 0 vertices OR exactly enough data for a given number of vertices.
                Debug.Assert(vbCounts[i] == 0 || VertexBuffers[i].Length % vbCounts[i] == 0);

                vbArrays[i] = new SolidMeshVertex?[vbCounts[i]];
            }

            // Reading vertices from buffers...
            foreach (var solidObjectMaterial in Materials)
            {
                var vertexBuffer = vbStreams[solidObjectMaterial.VertexStreamIndex];
                var numVerts = solidObjectMaterial.NumVerts == 0 ? vbCounts[solidObjectMaterial.VertexStreamIndex] : solidObjectMaterial.NumVerts;
                var stride = (int)(vertexBuffer.BaseStream.Length / vbCounts[solidObjectMaterial.VertexStreamIndex]);
                var vbStartPos = vertexBuffer.BaseStream.Position;
                var vbOffset = vbOffsets[solidObjectMaterial.VertexStreamIndex];

                for (var j = 0; j < numVerts; j++)
                {
                    // Make sure we don't end up reading more than we need to
                    if (vertexBuffer.BaseStream.Position >= vertexBuffer.BaseStream.Length
                        || vbArrays[solidObjectMaterial.VertexStreamIndex][vbOffset + j] != null)
                    {
                        break;
                    }

                    vbArrays[solidObjectMaterial.VertexStreamIndex][vbOffset + j] =
                        GetVertex(vertexBuffer, solidObjectMaterial, stride);
                    // Ensure we read exactly one vertex
                    Debug.Assert(vertexBuffer.BaseStream.Position - (vbStartPos + j * stride) == stride);
                }

                vbOffsets[solidObjectMaterial.VertexStreamIndex] += numVerts;
            }

            for (var i = 0; i < vbArrays.Length; i++)
            {
                ProcessVertices(ref vbArrays[i], i);
            }

            // Loading vertices into materials...
            foreach (var solidObjectMaterial in Materials)
            {
                var vertexStreamIndex = solidObjectMaterial.VertexStreamIndex;

                if (solidObjectMaterial.Indices.Any())
                {
                    var maxReferencedVertex = solidObjectMaterial.Indices.Max();
                    solidObjectMaterial.Vertices = new SolidMeshVertex[maxReferencedVertex + 1];

                    for (var j = 0; j <= maxReferencedVertex; j++)
                    {
                        var solidMeshVertex = vbArrays[vertexStreamIndex][j];
                        solidObjectMaterial.Vertices[j] = solidMeshVertex ?? throw new NullReferenceException($"Object {Name}: vertex buffer {vertexStreamIndex} has no vertex at index {j}");
                    }

                    // Validate material indices
                    Debug.Assert(solidObjectMaterial.Indices.All(t => t < solidObjectMaterial.Vertices.Length));
                }
                else
                {
                    solidObjectMaterial.Vertices = Array.Empty<SolidMeshVertex>();
                }
            }

            // Clean up vertex buffers, which are no longer needed
            VertexBuffers.Clear();
        }
    }

    public class SolidList : ChunkManager.BasicResource
    {
        public string PipelinePath { get; set; }

        public string Filename { get; set; }

        public List<SolidObject> Objects { get; } = new List<SolidObject>();
    }
}
