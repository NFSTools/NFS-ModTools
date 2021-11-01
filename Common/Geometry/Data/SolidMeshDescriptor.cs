namespace Common.Geometry.Data
{
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
}