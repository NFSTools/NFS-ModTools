using System.Numerics;

namespace Common.Geometry.Data
{
    public class SolidObjectMaterial
    {
        public Vector3 MinPoint { get; set; }

        public Vector3 MaxPoint { get; set; }

        public uint Hash { get; set; }

        public uint Flags { get; set; }

        public uint NumVerts { get; set; }

        public int VertexSetIndex { get; set; }

        public uint NumIndices { get; set; } // NumTris * 3

        public uint DiffuseTextureHash { get; set; }
        public uint? NormalTextureHash { get; set; }
        public uint? SpecularTextureHash { get; set; }

        // Second slot from the primary TextureNumber/TextureHashes array (index-based),
        // present when NumTextures > 1. Likely candidate for the blend-mask texture on
        // grass/dirt/rock/road effects, but the role isn't empirically confirmed yet.
        public uint? SecondaryTextureHash { get; set; }

        // Raw hashes from the separate TextureNameMaterial array (10 slots), captured
        // uninterpreted - role of each slot is not yet known.
        public uint[] MaterialTextureHashes { get; set; }


        public string Name { get; set; }
        public ushort[] Indices { get; set; }
    }
}