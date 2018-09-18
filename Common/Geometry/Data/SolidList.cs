using System.Collections.Generic;

namespace Common.Geometry.Data
{
    public class SimpleMatrix
    {
        public float[,] Data;

        public float this[int i, int j]
        {
            get => Data[i, j];
            set => Data[i, j] = value;
        }
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
        public ushort Vtx1;
        public ushort Vtx2;
        public ushort Vtx3;

        public ushort Shift1, Shift2, Shift3;

        public ushort[] ToArray()
        {
            return new[] { Vtx1, Vtx2, Vtx3 };
        }

        public ushort[] ShiftedArray()
        {
            return new[] {Shift1, Shift2, Shift3};
        }
    }

    public class SolidObjectMaterial
    {
        public SimpleVector3 MinPoint { get; set; }

        public SimpleVector3 MaxPoint { get; set; }

        //public byte[] TextureIndices { get; set; }

        //public byte ShaderIndex { get; set; }

        public uint Hash { get; set; }

        public uint Flags { get; set; }

        public uint NumVerts { get; set; }

        public uint NumTris { get; set; }

        //public uint TriOffset { get; set; }

        public int VertexStreamIndex { get; set; }

        public uint NumIndices { get; set; } // NumTris * 3

        public uint TextureHash { get; set; }

        public string Name { get; set; }
    }

    public class MostWantedMaterial : SolidObjectMaterial
    {
        public byte[] TextureIndices { get; set; }

        public byte ShaderIndex { get; set; }

        public uint TriOffset { get; set; }
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

    public class SolidObject : ChunkManager.BasicResource
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

        public List<uint> TextureHashes { get; } = new List<uint>();

        public Dictionary<int, List<ushort[]>> MaterialFaces { get; } = new Dictionary<int, List<ushort[]>>();

        public List<VertexBuffer> VertexBuffers { get; } = new List<VertexBuffer>();

        public List<SolidMeshFace> Faces { get; } = new List<SolidMeshFace>();

        /// <summary>
        /// Process the model data.
        /// </summary>
        public void PostProcessing()
        {
            var totalTris = NumTris;

            if (totalTris == 0)
            {
                totalTris = MeshDescriptor.NumIndices / 3;
            }

            var numVerts = 0u;
            var curFaceIdx = 0;
            var streamCountMap = new Dictionary<int, uint>();

            for (var i = 0; i < VertexBuffers.Count; i++)
            {
                streamCountMap[i] = 0;
            }

            for (var i = 0; i < Materials.Count; i++)
            //foreach (var material in Materials)
            {
                MaterialFaces[i] = new List<ushort[]>();

                var material = Materials[i];

                streamCountMap[material.VertexStreamIndex] += material.NumVerts;
            }

            for (var i = 0; i < Materials.Count; i++)
            {
                var material = Materials[i];
                var streamIdx = material.VertexStreamIndex;
                var stream = VertexBuffers[streamIdx];
                var stride = (int)(stream.Data.Count / streamCountMap[streamIdx]);
                var shift = numVerts - stream.Position / stride;

                for (var j = 0; j < streamCountMap[streamIdx]; ++j)
                {
                    if (stream.Position >= stream.Data.Count) break;

                    var x = stream.Data[stream.Position];
                    var y = stream.Data[stream.Position + 1];
                    var z = stream.Data[stream.Position + 2];
                    var u = stream.Data[stream.Position + 5];
                    var v = stream.Data[stream.Position + 6];

                    stream.Position += stride;

                    Vertices.Add(new SolidMeshVertex
                    {
                        X = x,
                        Y = y,
                        Z = z,
                        U = u,
                        V = v
                    });

                    numVerts++;
                }

                var triCount = material.NumTris;

                if (triCount == 0)
                {
                    triCount = material.NumIndices / 3;
                }

                for (var j = 0; j < triCount; j++)
                {
                    var faceIdx = curFaceIdx + j;
                    var face = Faces[faceIdx];

                    if (face.Vtx1 != face.Vtx2
                        && face.Vtx1 != face.Vtx3
                        && face.Vtx2 != face.Vtx3)
                    {
                        var origFace = new[]
                        {
                            face.Vtx1,
                            face.Vtx2,
                            face.Vtx3
                        };

                        Faces[faceIdx].Shift1 = (ushort)(shift + origFace[0]);
                        Faces[faceIdx].Shift2 = (ushort)(shift + origFace[2]);
                        Faces[faceIdx].Shift3 = (ushort)(shift + origFace[1]);
                    }

                    MaterialFaces[i].Add(Faces[faceIdx].ShiftedArray());
                }

                curFaceIdx += (int)triCount;

                if (curFaceIdx >= totalTris) break;
            }

            // cleanup data
            VertexBuffers.Clear();
        }
    }

    public class SolidList : ChunkManager.BasicResource
    {
        public string PipelinePath { get; set; }

        public string ClassType { get; set; }

        public int ObjectCount { get; set; }

        public List<SolidObject> Objects { get; } = new List<SolidObject>();
    }
}
