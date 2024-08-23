using System.Collections.Generic;

namespace Common.Geometry.Data;

public class World09Object : SolidObject, IMorphableSolid
{
    public World09Object()
    {
        // MorphLists = new List<List<MorphInfo>>();
        // MorphMatrices = new List<Matrix4x4>();
        MorphTargets = new List<uint>();
    }

    // public List<List<MorphInfo>> MorphLists { get; set; }
    // public List<Matrix4x4> MorphMatrices { get; set; }

    // public struct MorphInfo
    // {
    //     // morph range is [VertexStartIndex, VertexEndIndex]
    //     public int VertexStartIndex;
    //     public int VertexEndIndex;
    //     public int MorphMatrixIndex;
    // }
    public List<uint> MorphTargets { get; }
}