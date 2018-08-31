using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GeoEd.Data;

namespace GeoEd
{
    public abstract class SolidListManager
    {
        protected SolidListManager() { }

        /// <summary>
        /// Read a solid list from the given binary stream.
        /// </summary>
        /// <param name="br"></param>
        /// <param name="containerSize"></param>
        /// <returns></returns>
        public abstract SolidList ReadSolidList(BinaryReader br, uint containerSize);

        protected abstract void ReadChunks(BinaryReader br, uint containerSize);
    }
}
