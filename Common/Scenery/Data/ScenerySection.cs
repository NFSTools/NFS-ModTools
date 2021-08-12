using System.Collections.Generic;
using System.Numerics;

namespace Common.Scenery.Data
{
    public class SceneryInfo
    {
        public string Name { get; set; }
        public uint SolidKey { get; set; }
        public bool IsDeinstanced { get; set; }
    }

    public class SceneryInstance
    {
        public int InfoIndex { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Scale { get; set; }
        public Quaternion Rotation { get; set; }
    }
    
    public class ScenerySection : ChunkManager.BasicResource
    {
        public int SectionNumber { get; set; }
        public List<SceneryInfo> Infos { get; set; } = new List<SceneryInfo>();
        public List<SceneryInstance> Instances { get; set; } = new List<SceneryInstance>();
    }
}