using System.Collections.Generic;
using System.Numerics;

namespace Common.Geometry.Data
{
    public abstract class SolidObject : BasicResource
    {
        public static IEqualityComparer<SolidObject> HashComparer { get; } = new HashEqualityComparer();

        public string Name { get; set; }

        public Matrix4x4 Transform { get; set; }

        public Vector3 MinPoint { get; set; }

        public Vector3 MaxPoint { get; set; }

        public uint Hash { get; set; }

        public List<SolidObjectMaterial> Materials { get; } = new();

        public List<List<SolidMeshVertex>> VertexSets { get; } = new();

        public List<uint> TextureHashes { get; } = new();

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
    }
}