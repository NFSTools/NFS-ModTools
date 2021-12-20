using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;

namespace Common.Geometry.Data
{
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
                return (int)obj.Hash;
            }
        }

        public static IEqualityComparer<SolidObject> HashComparer { get; } = new HashEqualityComparer();

        public string Name { get; set; }

        public Matrix4x4 Transform { get; set; }

        public Vector3 MinPoint { get; set; }

        public Vector3 MaxPoint { get; set; }

        public uint Hash { get; set; }

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
                var vbIndex = solidObjectMaterial.VertexStreamIndex;
                var vbStream = vbStreams[vbIndex];
                var vbVertexCount = vbCounts[vbIndex];
                var vbOffset = vbOffsets[vbIndex];
                var vbStride = (int)(vbStream.BaseStream.Length / vbVertexCount);

                var numVerts = solidObjectMaterial.NumVerts == 0
                    ? vbVertexCount
                    : solidObjectMaterial.NumVerts;

                if (vbOffset < vbVertexCount)
                {
                    var vbStartPos = vbStream.BaseStream.Position;
                    
                    for (var i = 0; i < numVerts; i++)
                    {
                        Debug.Assert(vbStream.BaseStream.Position - vbStartPos == vbStride * i, "vbStream.BaseStream.Position - vbStartPos == vbStride * i");
                        vbArrays[vbIndex][vbOffset + i] = GetVertex(vbStream, solidObjectMaterial, vbStride);
                    }

                    vbOffsets[vbIndex] += numVerts;
                } else if (vbOffset != vbVertexCount)
                {
                    throw new Exception($"Vertex buffer read is in a weird state. vbOffset={vbOffset} vbVertexCount={vbVertexCount}");
                }
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
                    var meshVertices = vbArrays[vertexStreamIndex];
                    var maxReferencedVertex = solidObjectMaterial.Indices.Max();
                    solidObjectMaterial.Vertices = new SolidMeshVertex[maxReferencedVertex + 1];

                    for (var j = 0; j <= maxReferencedVertex; j++)
                    {
                        var solidMeshVertex = meshVertices[j];
                        solidObjectMaterial.Vertices[j] = solidMeshVertex ??
                                                          throw new NullReferenceException(
                                                              $"Object {Name}: vertex buffer {vertexStreamIndex} has no vertex at index {j}");
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
}