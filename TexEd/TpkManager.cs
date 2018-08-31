using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TexEd.Data;

namespace TexEd
{
    public abstract class TpkManager
    {
        /// <summary>
        /// Read a texture pack from the given binary stream.
        /// </summary>
        /// <param name="br"></param>
        /// <param name="containerSize"></param>
        /// <returns></returns>
        public abstract TexturePack ReadTexturePack(BinaryReader br, uint containerSize);

        protected abstract void ReadChunks(BinaryReader br, uint containerSize);
    }
}
