using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StreamEd.Data
{
    public class StreamSection
    {
        public string Name { get; set; }

        public uint Hash { get; set; }

        public uint Number { get; set; }

        public Vector3 Position { get; set; }

        public uint Offset { get; set; }

        public uint Size { get; set; }

        public uint OtherSize { get; set; }
    }

    public class LocationBundle
    {
        public string Name { get; set; }

        public string File { get; set; }

        public List<StreamSection> Sections { get; set; }
    }
}
