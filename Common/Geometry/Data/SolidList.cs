using System.Collections.Generic;

namespace Common.Geometry.Data
{
    public class SolidList : BasicResource
    {
        public string Filename { get; set; }

        public string GroupName { get; set; }

        public List<SolidObject> Objects { get; } = new List<SolidObject>();
    }
}