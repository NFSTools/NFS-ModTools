using System;

namespace FBXSharp
{
    [Flags]
    public enum LoadFlags
    {
        None = 0,
        Triangulate = 1 << 0,
        RemapSubmeshes = 1 << 1,
        IgnoreGeometry = 1 << 2,
        IgnoreBlendShapes = 1 << 3,
        LoadVideoFiles = 1 << 4
    }
}