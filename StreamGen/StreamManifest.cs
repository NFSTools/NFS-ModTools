using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.TrackStream.Data;

namespace StreamGen
{
    public class StreamManifestSection
    {
        /// <summary>
        /// e.g. X0, Y0, Z0
        /// in NFSW, Z0 is basically the skybox and holding area for objects that are in the air
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Position in the world
        /// </summary>
        public Vector3 Position { get; set; }
    }

    public class StreamManifest
    {
        public string Name { get; set; }
        public List<StreamManifestSection> Sections { get; set; }
    }
}
