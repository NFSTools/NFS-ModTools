using System.Numerics;

namespace Common.Geometry.Data
{
    public struct SolidMeshVertex
    {
        // REQUIRED attributes
        public Vector3 Position { get; set; }
        public Vector2 TexCoords { get; set; }

        // OPTIONAL attributes
        public Vector3? Normal { get; set; }
        public Vector3? Tangent { get; set; }

        public uint? Color { get; set; }
    }
}