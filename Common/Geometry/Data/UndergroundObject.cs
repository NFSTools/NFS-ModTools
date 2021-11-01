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
}