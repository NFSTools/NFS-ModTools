using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;

namespace Common.Geometry.Data
{
    public abstract class SolidObject : BasicResource
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
        /// Read a vertex for a material from a binary stream.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="material"></param>
        /// <param name="stride"></param>
        /// <returns></returns>
        protected abstract SolidMeshVertex GetVertex(BinaryReader reader, SolidObjectMaterial material, int stride);

        /// <summary>
        /// Perform post-processing operations on a vertex array
        /// </summary>
        /// <param name="vertices">The vertex array</param>
        /// <param name="id">The ID of the vertex array</param>
        protected virtual void ProcessVertices(ref SolidMeshVertex[] vertices, int id)
        {
            //
        }

        /// <summary>
        /// Process the model data.
        /// </summary>
        public virtual void PostProcessing()
        {
            var vertexBuffersCount = VertexBuffers.Count;
            
            // Bookkeeping array: how many vertices are in each vertex buffer?
            var vbCounts = new uint[vertexBuffersCount];
            
            // Bookkeeping array: how many vertices have been consumed from each vertex buffer?
            var vbOffsets = new uint[vertexBuffersCount];
            
            // Vertex buffer data readers
            var vbReaders = new BinaryReader[vertexBuffersCount];
            
            // Vertex arrays
            var vbArrays = new SolidMeshVertex[vertexBuffersCount][];

            // Set up vertex buffer readers
            for (var i = 0; i < vbReaders.Length; i++)
            {
                vbReaders[i] = new BinaryReader(new MemoryStream(VertexBuffers[i]));
            }

            // Filling in vbCounts...
            if (vertexBuffersCount == 1)
            {
                // The mesh descriptor tells us how many vertices exist.
                // Since we only have one vertex buffer, we can use the info
                // from the mesh descriptor instead of doing the material loop
                // seen after this block.
                vbCounts[0] = MeshDescriptor.NumVerts;
            }
            else if (vertexBuffersCount > 1)
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
            for (var i = 0; i < vbReaders.Length; i++)
            {
                // To avoid a division-by-zero exception, we allow a vertex buffer to have
                // EITHER 0 vertices OR exactly enough data for a given number of vertices.
                Debug.Assert(vbCounts[i] == 0 || VertexBuffers[i].Length % vbCounts[i] == 0);

                vbArrays[i] = new SolidMeshVertex[vbCounts[i]];
            }

            // Reading vertices from buffers...
            foreach (var solidObjectMaterial in Materials)
            {
                var vbIndex = solidObjectMaterial.VertexStreamIndex;
                var vbStream = vbReaders[vbIndex];
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
                        solidObjectMaterial.Vertices[j] = meshVertices[j];
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