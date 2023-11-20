using System.Collections.Generic;

namespace Common.Geometry.Data;

public interface IMorphableSolid
{
    List<uint> MorphTargets { get; }
}