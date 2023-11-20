using System.Collections.Generic;

namespace Common.Geometry.Data
{
    public class CarbonObject : SolidObject, IMorphableSolid
    {
        public List<uint> MorphTargets { get; } = new();
    }
}