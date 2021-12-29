using System;
using System.IO;

namespace Common.Geometry.Data
{
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

            InternalEffectID id = (InternalEffectID)cm.EffectId;

            switch (id)
            {
                case InternalEffectID.WorldShader:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    vertex.Normal = BinaryUtil.ReadVector3(reader);
                    vertex.Color = reader.ReadUInt32();
                    vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                    break;
                case InternalEffectID.skyshader:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    vertex.Normal = BinaryUtil.ReadVector3(reader);
                    vertex.Color = reader.ReadUInt32();
                    vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                    reader.BaseStream.Position += 8; // TODO: What's this additional D3DDECLUSAGE_TEXCOORD element?
                    break;
                case InternalEffectID.WorldNormalMap:
                case InternalEffectID.WorldReflectShader:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    vertex.Normal = BinaryUtil.ReadVector3(reader);
                    vertex.Color = reader.ReadUInt32();
                    vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                    vertex.Tangent = BinaryUtil.ReadVector3(reader);
                    reader.BaseStream.Position += 4; // skip W component of tangent vector
                    break;
                case InternalEffectID.CarShader:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    vertex.Normal = BinaryUtil.ReadVector3(reader);
                    vertex.Color = reader.ReadUInt32();
                    vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                    vertex.Tangent = BinaryUtil.ReadVector3(reader);
                    break;
                case InternalEffectID.CARNORMALMAP:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                    vertex.Color = reader.ReadUInt32();
                    vertex.Normal = BinaryUtil.ReadVector3(reader);
                    vertex.Tangent = BinaryUtil.ReadVector3(reader);
                    break;
                case InternalEffectID.GLASS_REFLECT:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                    vertex.Color = reader.ReadUInt32();
                    vertex.Normal = BinaryUtil.ReadVector3(reader);
                    break;
                case InternalEffectID.WATER:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    vertex.Color = reader.ReadUInt32();
                    vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                    vertex.Normal = BinaryUtil.ReadVector3(reader);
                    break;
                default:
                    throw new Exception($"Unsupported effect in object {Name}: {id}");
            }

            return vertex;
        }
    }
}