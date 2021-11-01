using System;
using System.IO;

namespace Common.Geometry.Data
{
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

            InternalEffectID id = (InternalEffectID) mwm.EffectId;

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
}