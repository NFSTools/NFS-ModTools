using System;
using System.IO;

namespace Common.Geometry.Data
{
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

            switch ((EffectID)psm.EffectId)
            {
                case EffectID.WORLDBAKEDLIGHTING:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    vertex.Color = reader.ReadUInt32();
                    vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                    reader.BaseStream.Position += 16;
                    vertex.Normal = BinaryUtil.ReadNormal(reader, true);
                    break;
                case EffectID.WORLD:
                case EffectID.SKY:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    vertex.Normal = BinaryUtil.ReadNormal(reader);
                    vertex.Color = reader.ReadUInt32();
                    vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                    reader.BaseStream.Position += 8; // TODO: what is this second D3DDECLUSAGE_TEXCOORD element?
                    break;
                case EffectID.WORLDNORMALMAP:
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
                case EffectID.WorldDepthShader:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    vertex.Normal = BinaryUtil.ReadNormal(reader);
                    vertex.Color = reader.ReadUInt32();
                    vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                    break;
                case EffectID.TREELEAVES:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    vertex.Color = reader.ReadUInt32();
                    vertex.Normal = BinaryUtil.ReadNormal(reader);
                    // TODO: COLLADA supports tangent vectors, so we should eventually read them
                    //> elements[3]: type=D3DDECLTYPE_FLOAT4 usage=D3DDECLUSAGE_TANGENT size=16 offset=0x1c
                    reader.BaseStream.Position += 0x10;
                    vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                    break;
                case EffectID.WORLDCONSTANT:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                    vertex.Color = reader.ReadUInt32();
                    break;
                case EffectID.ROAD:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                    reader.BaseStream.Position += 28;
                    vertex.Color = reader.ReadUInt32();
                    break;
                case EffectID.TERRAIN:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                    reader.BaseStream.Position += 16;
                    vertex.Normal = BinaryUtil.ReadNormal(reader, true);
                    // TODO: read packed tangent vector (4 short-components)
                    reader.BaseStream.Position += 8;
                    vertex.Color = reader.ReadUInt32();
                    reader.BaseStream.Position +=
                        8; // TODO: what are these other D3DDECLUSAGE_TEXCOORD elements? (2x D3DDECLTYPE_UBYTE4)
                    break;
                case EffectID.GRASSTERRAIN:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    vertex.TexCoords = BinaryUtil.ReadVector2(reader);
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
                    vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                    reader.BaseStream.Position += 8; // TODO: what is this second D3DDECLUSAGE_TEXCOORD element?
                    break;
                case EffectID.GRASSCARD:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    vertex.Color = reader.ReadUInt32();
                    vertex.Normal = BinaryUtil.ReadNormal(reader, true);
                    // TODO: read packed tangent vector (4 short-components)
                    reader.BaseStream.Position += 8;
                    vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                    reader.BaseStream.Position += 8; // TODO: what is this second D3DDECLUSAGE_TEXCOORD element?
                    break;
                case EffectID.CAR:
                case EffectID.CARNORMALMAP:
                case EffectID.CARVINYL:
                    vertex.Position = BinaryUtil.ReadNormal(reader, true);
                    vertex.TexCoords = BinaryUtil.ReadShort2N(reader);
                    vertex.Color = reader.ReadUInt32();
                    vertex.Normal = BinaryUtil.ReadNormal(reader, true);
                    vertex.Tangent = BinaryUtil.ReadNormal(reader, true);
                    break;
                case EffectID.ALWAYSFACING:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    vertex.Color = reader.ReadUInt32();
                    vertex.Normal = BinaryUtil.ReadNormal(reader, true);
                    // TODO: read packed tangent vector (4 short-components)
                    reader.BaseStream.Position += 8;
                    vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                    break;
                case EffectID.STANDARD:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    vertex.Color = reader.ReadUInt32();
                    vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                    reader.BaseStream.Position += 8; // todo: what's this?
                    break;
                case EffectID.TUNNEL:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                    reader.BaseStream.Position += 8; // todo: what's this?
                    reader.BaseStream.Position += 4; // todo: what's this?
                    reader.BaseStream.Position += 4; // todo: what's this?
                    vertex.Color = reader.ReadUInt32();
                    vertex.Normal = BinaryUtil.ReadNormal(reader, true);
                    break;
                case EffectID.ROADLIGHTMAP:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                    reader.BaseStream.Position += 8; // todo: what's this?
                    reader.BaseStream.Position += 8; // todo: what's this?
                    reader.BaseStream.Position += 4; // todo: what's this?
                    vertex.Normal = BinaryUtil.ReadNormal(reader, true);
                    vertex.Color = reader.ReadUInt32();
                    break;
                case EffectID.WATER:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    vertex.Color = reader.ReadUInt32();
                    vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                    break;
                case EffectID.WORLDBONE:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    vertex.Normal = BinaryUtil.ReadNormal(reader);
                    vertex.Color = reader.ReadUInt32();
                    vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                    vertex.BlendWeight = BinaryUtil.ReadVector3(reader);
                    vertex.BlendIndices = BinaryUtil.ReadVector3(reader);
                    vertex.Tangent = BinaryUtil.ReadVector3(reader);
                    break;
                default:
                    throw new Exception($"Unsupported effect in object {Name}: {psm.EffectId}");
            }

            return vertex;
        }
    }
}