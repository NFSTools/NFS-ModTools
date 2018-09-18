using System.IO;
using Common.Geometry.Data;

namespace Common.Geometry
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

        /// <summary>
        /// Write a solid list to a file.
        /// </summary>
        /// <param name="chunkStream"></param>
        /// <param name="solidList"></param>
        public abstract void WriteSolidList(ChunkStream chunkStream, SolidList solidList);
    }
}
