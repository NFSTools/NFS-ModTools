using System.Collections.Generic;

namespace Common.Geometry.Data
{
    public class SolidList : ChunkManager.BasicResource
    {
        public string PipelinePath { get; set; }

        public string Filename { get; set; }

        public List<SolidObject> Objects { get; } = new List<SolidObject>();
    }
}
