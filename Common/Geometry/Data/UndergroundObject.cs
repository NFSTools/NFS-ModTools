using System;
using System.IO;

namespace Common.Geometry.Data
{
    public class UndergroundObject : SolidObject
    {
        protected override SolidMeshVertex GetVertex(BinaryReader reader, SolidObjectMaterial material, int stride)
        {
            SolidMeshVertex vertex;
            switch (stride)
            {
                case 60:
                    vertex = new SolidMeshVertex
                    {
                        Position = BinaryUtil.ReadVector3(reader),
                        Normal = BinaryUtil.ReadVector3(reader),
                        Color = reader.ReadUInt32(),
                        TexCoords = BinaryUtil.ReadVector2(reader),
                        BlendWeight = BinaryUtil.ReadVector3(reader),
                        BlendIndices = BinaryUtil.ReadVector3(reader),
                    };
                    break;
                // position (12 bytes) + normal (12 bytes) + color (4 bytes) + tex coords (8 bytes)
                case 36:
                    vertex = new SolidMeshVertex
                    {
                        Position = BinaryUtil.ReadVector3(reader),
                        Normal = BinaryUtil.ReadVector3(reader),
                        Color = reader.ReadUInt32(),
                        TexCoords = BinaryUtil.ReadVector2(reader)
                    };
                    break;
                // position (12 bytes) + color (4 bytes) + tex coords (8 bytes)
                case 24:
                    vertex = new SolidMeshVertex
                    {
                        Position = BinaryUtil.ReadVector3(reader),
                        Color = reader.ReadUInt32(),
                        TexCoords = BinaryUtil.ReadVector2(reader)
                    };
                    break;
                default:
                    throw new Exception($"Cannot handle vertex size: {stride}");
            }

            return vertex;
        }
    }
}