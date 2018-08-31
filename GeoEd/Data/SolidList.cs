using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeoEd.Data
{
    public class SimpleMatrix
    {
        public float[,] Data;
    }

    public class SimpleVector3
    {
        public float X;
        public float Y;
        public float Z;

        public SimpleVector3(float x, float y, float z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }
    }

    public class SimpleVector4
    {
        public float X;
        public float Y;
        public float Z;
        public float D;

        public SimpleVector4(float x, float y, float z, float d)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
            this.D = d;
        }
    }

    public class VertexBuffer
    {
        public List<float> Data { get; } = new List<float>();

        public int Position { get; set; }
    }

    public class SolidMeshFace
    {
        public ushort Vtx1, Vtx2, Vtx3;
    }

    public class SolidObjectMaterial
    {
        public SimpleVector3 MinPoint { get; set; }

        public SimpleVector3 MaxPoint { get; set; }

        public byte[] TextureIndices { get; set; }

        public byte ShaderIndex { get; set; }

        public uint Flags { get; set; }

        public uint NumVerts { get; set; }

        // value from material struct
        public int Unknown1 { get; set; }

        public uint NumTris { get; set; }

        public uint TriOffset { get; set; }

        public int VertexStreamIndex { get; set; }

        public uint Length { get; set; } // NumTris * 3

        public string Name { get; set; }
    }

    public class SolidMeshVertex
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float NormalX { get; set; }
        public float NormalY { get; set; }
        public float NormalZ { get; set; }
        public float U { get; set; }
        public float V { get; set; }
        public int Color { get; set; }
    }

    public class SolidMeshDescriptor
    {
        public uint Flags { get; set; }

        public uint NumMats { get; set; }

        public uint NumVertexStreams { get; set; }

        public uint NumIndices { get; set; } // NumTris * 3

        // dynamically computed
        public bool HasNormals { get; set; }

        // dynamically computed
        public uint NumVerts { get; set; }
    }

    public class SolidObject
    {
        public string Name { get; set; }

        public SimpleMatrix Transform { get; set; }

        public SimpleVector4 MinPoint { get; set; }

        public SimpleVector4 MaxPoint { get; set; }

        public uint Hash { get; set; }

        public uint NumTextures { get; set; }

        public uint NumShaders { get; set; }

        public uint NumTris { get; set; }

        public SolidMeshDescriptor MeshDescriptor { get; set; }

        public List<SolidObjectMaterial> Materials { get; } = new List<SolidObjectMaterial>();

        public List<SolidMeshVertex> Vertices { get; } = new List<SolidMeshVertex>();

        public List<VertexBuffer> VertexBuffers { get; } = new List<VertexBuffer>();

        public List<SolidMeshFace> Faces { get; } = new List<SolidMeshFace>();
    }

    public class SolidList
    {
        public string PipelinePath { get; set; }

        public string ClassType { get; set; }

        public int ObjectCount { get; set; }

        public List<SolidObject> Objects { get; } = new List<SolidObject>();
    }
}
