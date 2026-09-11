using System.Numerics;

namespace Common.Geometry.Data
{
    public struct SolidMeshVertex
    {
        // REQUIRED attributes
        public Vector3 Position { get; set; }
        public Vector2 TexCoords { get; set; }

        // OPTIONAL attributes
        public Vector2? TexCoords1 { get; set; }
        public Vector2? TexCoords2 { get; set; }

        public Vector3? Normal { get; set; }
        public Vector3? Tangent { get; set; }
        public Vector3? BlendWeight { get; set; }
        public Vector3? BlendIndices { get; set; }

        public uint? Color { get; set; }
        public uint? Color2 { get; set; }

        // Raw 4-byte blob captured from the vertex stream; format TBD
        public byte[] Unknown_xb4;

        // Raw 8-byte blob captured from the vertex stream; format TBD
        public byte[] Unknown_Tan0s;
    }
}