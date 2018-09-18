using System.IO;
using Common.Textures.Data;

namespace Common.Textures
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
