using System.Collections.Generic;
using System.Numerics;

namespace Common.Geometry.Data
{
    public class World15Object : SolidObject
    {
        public World15Object()
        {
            MorphLists = new List<List<MorphInfo>>();
            MorphMatrices = new List<Matrix4x4>();
        }

        public List<List<MorphInfo>> MorphLists { get; set; }
        public List<Matrix4x4> MorphMatrices { get; set; }

        public struct MorphInfo
        {
            // morph range is [VertexStartIndex, VertexEndIndex]
            public int VertexStartIndex;
            public int VertexEndIndex;
            public int MorphMatrixIndex;
        }
    }
}