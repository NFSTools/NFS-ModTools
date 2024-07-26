﻿using System.Numerics;

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

        public string Name { get; set; }
        public ushort[] Indices { get; set; }
    }
}