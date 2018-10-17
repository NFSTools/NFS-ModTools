using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

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

        public float[] ToArray()
        {
            return new[] { this.X, this.Y, this.Z };
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

        public float[] ToArray()
        {
            return new[] { this.X, this.Y, this.Z, this.D };
        }
    }

    public struct VertexBuffer
    {
        public float[] Data;

        public int Position { get; set; }
    }

    public struct SolidMeshFace
    {
        public byte MaterialIndex;

        public ushort Vtx1;
        public ushort Vtx2;
        public ushort Vtx3;

        public ushort[] ToArray()
        {
            return new[] { Vtx1, Vtx2, Vtx3 };
        }
    }

    public class SolidObjectMaterial
    {
        public SimpleVector3 MinPoint { get; set; }

        public SimpleVector3 MaxPoint { get; set; }

        public uint Hash { get; set; }

        public uint Flags { get; set; }

        public uint NumVerts { get; set; }

        public uint NumTris { get; set; }

        //public uint TriOffset { get; set; }

        public int VertexStreamIndex { get; set; }

        public uint NumIndices { get; set; } // NumTris * 3

        public uint TextureHash { get; set; }

        public byte TextureIndex { get; set; }

        public string Name { get; set; }
    }

    public class MostWantedMaterial : SolidObjectMaterial
    {
        public byte[] TextureIndices { get; set; }

        public byte ShaderIndex { get; set; }

        public uint TriOffset { get; set; }
        public uint Unknown1 { get; set; }
    }

    public class Underground2Material : SolidObjectMaterial
    {
        public object RawStructure { get; set; }
    }

    public class CarbonMaterial : SolidObjectMaterial
    {
        public int Unknown1 { get; set; }
    }

    public class UndercoverMaterial : CarbonMaterial
    {
    }

    public class ProStreetMaterial : CarbonMaterial
    {
    }

    public class World15Material : CarbonMaterial
    {
    }

    public struct SolidMeshVertex
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
        public uint NumTris { get; set; }

        // dynamically computed
        public bool HasNormals { get; set; }

        // dynamically computed
        public uint NumVerts { get; set; }
    }

    public class UndergroundObject : SolidObject
    {
        public override SolidMeshVertex GetVertex(VertexBuffer buffer, SolidObjectMaterial material, int stride)
        {
            var uvBase = stride >= 9 ? 7 : 4;

            return new SolidMeshVertex
            {
                X = buffer.Data[buffer.Position],
                Y = buffer.Data[buffer.Position + 1],
                Z = buffer.Data[buffer.Position + 2],
                U = buffer.Data[buffer.Position + uvBase],
                V = -buffer.Data[buffer.Position + uvBase + 1]
            };
        }

        public override int ComputeStride()
        {
            return (int) (this.VertexBuffers[0].Data.Length / this.MeshDescriptor.NumVerts);
        }
    }

    public class Underground2Object : SolidObject
    {
        public override SolidMeshVertex GetVertex(VertexBuffer buffer, SolidObjectMaterial material, int stride)
        {
            var uvBase = stride == 9 ? 7 : 4;

            return new SolidMeshVertex
            {
                X = buffer.Data[buffer.Position],
                Y = buffer.Data[buffer.Position + 1],
                Z = buffer.Data[buffer.Position + 2],
                U = buffer.Data[buffer.Position + uvBase],
                V = -buffer.Data[buffer.Position + uvBase + 1],
            };
        }

        public override int ComputeStride()
        {
            return this.MeshDescriptor.HasNormals ? 9 : 6;
        }
    }

    public class MostWantedObject : SolidObject
    {
        public override SolidMeshVertex GetVertex(VertexBuffer buffer, SolidObjectMaterial material, int stride)
        {
            return new SolidMeshVertex
            {
                X = buffer.Data[buffer.Position],
                Y = buffer.Data[buffer.Position + 1],
                Z = buffer.Data[buffer.Position + 2],
                NormalX = buffer.Data[buffer.Position + 3],
                NormalY = buffer.Data[buffer.Position + 4],
                NormalZ = buffer.Data[buffer.Position + 5],
                U = buffer.Data[buffer.Position + 7],
                V = -buffer.Data[buffer.Position + 8]
            };
        }

        public override int ComputeStride()
        {
            return 0;
        }
    }

    public class CarbonObject : SolidObject
    {
        public override SolidMeshVertex GetVertex(VertexBuffer buffer, SolidObjectMaterial material, int stride)
        {
            return new SolidMeshVertex
            {
                X = buffer.Data[buffer.Position],
                Y = buffer.Data[buffer.Position + 1],
                Z = buffer.Data[buffer.Position + 2],
                U = buffer.Data[buffer.Position + 7],
                V = -buffer.Data[buffer.Position + 8]
            };
        }

        public override int ComputeStride()
        {
            return 0;
        }
    }

    public class ProStreetObject : SolidObject
    {
        private readonly bool _isTestObject;

        public ProStreetObject(bool isTestObject = false)
        {
            _isTestObject = isTestObject;
            RotationAngle = 90.0f;
        }

        public override SolidMeshVertex GetVertex(VertexBuffer buffer, SolidObjectMaterial material, int stride)
        {
            // IsCompressed ---> packed floats
            float x;
            float y;
            float z;
            float u;
            float v;

            var uvBase = 7;

            if (stride < 9)
            {
                uvBase = 3;
            }

            if (IsCompressed)
            {
                unsafe
                {
                    fixed (float* dataArray = buffer.Data)
                    {
                        x = BinaryUtil.GetPackedFloat(&dataArray[buffer.Position], 0);
                        y = BinaryUtil.GetPackedFloat(&dataArray[buffer.Position], 2);
                        z = BinaryUtil.GetPackedFloat(&dataArray[buffer.Position], 1);
                        u = -BinaryUtil.GetPackedFloat(&dataArray[buffer.Position + 2], 0);
                        v = -BinaryUtil.GetPackedFloat(&dataArray[buffer.Position + 2], 1);
                    }
                }
            }
            else
            {
                x = buffer.Data[buffer.Position];
                y = buffer.Data[buffer.Position + (_isTestObject ? 1 : 2)];
                z = buffer.Data[buffer.Position + (_isTestObject ? 2 : 1)];
                u = buffer.Data[buffer.Position + uvBase];
                v = buffer.Data[buffer.Position + uvBase + 1] * -1.0f;
            }

            return new SolidMeshVertex
            {
                X = x,
                Y = y,
                Z = z,
                U = u,
                V = v
            };

            //return new SolidMeshVertex
            //{
            //    X = buffer.Data[buffer.Position],
            //    Y = buffer.Data[buffer.Position + (_isTestObject ? 1 : 2)],
            //    Z = buffer.Data[buffer.Position + (_isTestObject ? 2 : 1)],
            //    U = buffer.Data[buffer.Position + uvBase],
            //    V = buffer.Data[buffer.Position + uvBase + 1] * -1.0f
            //};
        }

        public override int ComputeStride()
        {
            return 0;
        }
    }

    public class UndercoverObject : SolidObject
    {
        /// <inheritdoc />
        /// <summary>
        /// This thing is responsible for somehow deriving proper vertex data from
        /// a NFS:Undercover vertex buffer. Getting the coordinates is easy, but
        /// the hard part is getting the texture coordinates. Can it be done? Who knnows?
        /// </summary>
        /// <remarks>And I didn't even mention the fact that there are wayyyy too many edge cases...</remarks>
        /// <remarks>I hate this game. Worse than E.T.</remarks>
        /// <param name="buffer"></param>
        /// <param name="material"></param>
        /// <param name="stride"></param>
        /// <returns></returns>
        public override SolidMeshVertex GetVertex(VertexBuffer buffer, SolidObjectMaterial material, int stride)
        {
            float x, y, z, u, v;

            // Vertices for compressed models are read in a different way.
            if (this.IsCompressed)
            {
                x = 0.0f;
                y = 0.0f;
                z = 0.0f;
                u = 0.0f;
                v = 0.0f;
            }
            else
            {
                // According to BlackBox logic, X-Z-Y is how the 3D coordinate system works.
                x = buffer.Data[buffer.Position];
                y = buffer.Data[buffer.Position + 2];
                z = buffer.Data[buffer.Position + 1];

                unsafe
                {
                    fixed (float* dataArray = buffer.Data)
                    {
                        u = BinaryUtil.GetPackedFloat(&dataArray[buffer.Position + 4], 0);
                        v = BinaryUtil.GetPackedFloat(&dataArray[buffer.Position + 4], 1);
                    }
                }
            }

            return new SolidMeshVertex
            {
                X = x,
                Y = y,
                Z = z,
                U = u,
                V = v
            };
        }

        public override int ComputeStride()
        {
            return 0;
        }
    }

    public class World15Object : SolidObject
    {
        public World15Object()
        {
            RotationAngle = 90.0f;
        }

        public override SolidMeshVertex GetVertex(VertexBuffer buffer, SolidObjectMaterial material, int stride)
        {
            // IsCompressed ---> packed floats
            float x;
            float y;
            float z;
            float u;
            float v;

            const int uvBase = 5;

            if (IsCompressed)
            {
                unsafe
                {
                    fixed (float* dataArray = buffer.Data)
                    {
                        x = BinaryUtil.GetPackedFloat(&dataArray[buffer.Position], 0);
                        y = BinaryUtil.GetPackedFloat(&dataArray[buffer.Position], 2);
                        z = BinaryUtil.GetPackedFloat(&dataArray[buffer.Position], 1);
                        u = BinaryUtil.GetPackedFloat(&dataArray[buffer.Position + 6], 0);
                        v = -BinaryUtil.GetPackedFloat(&dataArray[buffer.Position + 6], 1);
                    }
                }
            }
            else
            {
                x = buffer.Data[buffer.Position];
                y = buffer.Data[buffer.Position + 2];
                z = buffer.Data[buffer.Position + 1];
                u = buffer.Data[buffer.Position + uvBase];
                v = buffer.Data[buffer.Position + uvBase + 1] * -1.0f;
            }

            return new SolidMeshVertex
            {
                X = x,
                Y = y,
                Z = z,
                U = u,
                V = v
            };
        }

        public override int ComputeStride()
        {
            return 0;
        }
    }

    public abstract class SolidObject : ChunkManager.BasicResource
    {
        public string Name { get; set; }

        public uint Flags { get; set; }

        public bool IsCompressed { get; set; } = false;

        public bool EnableTransform { get; set; } = true;

        public SimpleMatrix Transform { get; set; }

        public SimpleVector4 MinPoint { get; set; }

        public SimpleVector4 MaxPoint { get; set; }

        public float RotationAngle { get; set; } = 0.0f;

        public uint Hash { get; set; }

        public uint NumTextures { get; set; }

        public uint NumShaders { get; set; }

        public uint NumTris { get; set; }

        public SolidMeshDescriptor MeshDescriptor { get; set; }

        public List<SolidObjectMaterial> Materials { get; } = new List<SolidObjectMaterial>();

        public List<uint> TextureHashes { get; } = new List<uint>();

        //public Dictionary<int, List<int>> MaterialFaces { get; } = new Dictionary<int, List<int>>();

        public List<VertexBuffer> VertexBuffers { get; } = new List<VertexBuffer>();

        public SolidMeshFace[] Faces = new SolidMeshFace[0];
        public SolidMeshVertex[] Vertices = new SolidMeshVertex[0];

        public bool HasNormals => (MeshDescriptor.Flags & 0x000080) == 0x000080;

        /// <summary>
        /// Pull a vertex from the given buffer.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="material"></param>
        /// <param name="stride"></param>
        /// <returns></returns>
        public abstract SolidMeshVertex GetVertex(VertexBuffer buffer, SolidObjectMaterial material, int stride);

        public abstract int ComputeStride();

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

            var numVerts = 0;
            var curFaceIdx = 0;
            var streamCountMap = new Dictionary<int, uint>();

            for (var i = 0; i < VertexBuffers.Count; i++)
            {
                streamCountMap[i] = 0;
            }

            foreach (var material in Materials)
            {
                streamCountMap[material.VertexStreamIndex] += material.NumVerts;
            }

            var vertArraySize = (int) streamCountMap.Sum(p => p.Value);

            if (vertArraySize == 0)
            {
                if (VertexBuffers[0].Data.Length == 0)
                {
                    VertexBuffers.Clear();
                    streamCountMap.Clear();
                    return;
                }

                vertArraySize = VertexBuffers[0].Data.Length / ComputeStride();
            }

            Array.Resize(ref Vertices, vertArraySize);

            for (var i = 0; i < Materials.Count; i++)
            {
                var material = Materials[i];
                var streamIdx = material.VertexStreamIndex;
                var stream = VertexBuffers[streamIdx];
                var stride = ComputeStride();

                if (stride == 0)
                {
                    stride = (int) (stream.Data.Length / streamCountMap[streamIdx]);
                }
                else
                {
                    if (stream.Data.Length % stride != 0)
                    {
                        throw new Exception("Look over here!");
                    }
                }

                var shift = numVerts - stream.Position / stride;
                var vertCount = streamCountMap[streamIdx] == 0
                    ? (uint) (stream.Data.Length / stride)
                    : streamCountMap[streamIdx];

                for (var j = 0; j < vertCount; ++j)
                {
                    if (stream.Position >= stream.Data.Length) break;
                    var vertex = GetVertex(stream, material, stride);
                    stream.Position += stride;
                    Vertices[numVerts] = vertex;

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

                        Faces[faceIdx].Vtx1 = (ushort)(shift + origFace[0]);
                        Faces[faceIdx].Vtx2 = (ushort)(shift + origFace[2]);
                        Faces[faceIdx].Vtx3 = (ushort)(shift + origFace[1]);
                    }

                    Faces[faceIdx].MaterialIndex = (byte) i;
                }

                curFaceIdx += (int)triCount;

                VertexBuffers[streamIdx] = stream;
                if (curFaceIdx >= totalTris)
                {
                    break;
                }
            }

            streamCountMap.Clear();
            streamCountMap = null;
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
