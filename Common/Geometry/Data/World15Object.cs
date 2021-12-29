using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;

namespace Common.Geometry.Data
{
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

            InternalEffectID id = (InternalEffectID)wm.EffectId;

            switch (id)
            {
                case InternalEffectID.WorldShader:
                case InternalEffectID.GLASS_REFLECT:
                case InternalEffectID.WorldZBiasShader:
                case InternalEffectID.Tree:
                case InternalEffectID.WATER:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    vertex.Normal = BinaryUtil.ReadNormal(reader, true);
                    vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                    vertex.Color = reader.ReadUInt32(); // daytime color
                    reader.ReadUInt32(); // nighttime color
                    break;
                case InternalEffectID.WorldPrelitShader:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    vertex.Color = reader.ReadUInt32(); // daytime color
                    reader.ReadUInt32(); // nighttime color
                    vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                    break;
                case InternalEffectID.WorldZBiasPrelitShader:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    vertex.Normal = BinaryUtil.ReadNormal(reader, true);
                    vertex.Color = reader.ReadUInt32(); // daytime color
                    reader.ReadUInt32(); // nighttime color
                    vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                    break;
                case InternalEffectID.WorldNormalMap:
                case InternalEffectID.GLASS_REFLECTNM:
                case InternalEffectID.WorldRoadShader:
                case InternalEffectID.WorldFEShader:
                    vertex.Position = BinaryUtil.ReadVector3(reader);
                    vertex.Normal = BinaryUtil.ReadNormal(reader, true);
                    vertex.TexCoords = BinaryUtil.ReadVector2(reader);
                    vertex.Color = reader.ReadUInt32(); // daytime color
                    reader.ReadUInt32(); // nighttime color
                    vertex.Tangent = BinaryUtil.ReadNormal(reader, true);
                    break;
                case InternalEffectID.CarShader:
                case InternalEffectID.CARNORMALMAP:
                    vertex.Position = BinaryUtil.ReadNormal(reader, true) * 8;
                    vertex.TexCoords = new Vector2(reader.ReadInt16() / 4096f, reader.ReadInt16() / 4096f - 1);
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

        protected override void ProcessVertices(ref SolidMeshVertex[] vertices, int id)
        {
            base.ProcessVertices(ref vertices, id);

            // If we don't have a morph list for the stream, bail out
            if (this.MorphLists.Count <= id) return;

            var morphList = this.MorphLists[id];

            foreach (var morphInfo in morphList)
            {
                for (var i = morphInfo.VertexStartIndex;
                    i <= morphInfo.VertexEndIndex;
                    i++)
                {
                    vertices[i].Position = Vector3.Transform(
                        vertices[i].Position, 
                        this.MorphMatrices[morphInfo.MorphMatrixIndex]);
                }
            }
        }
    }
}