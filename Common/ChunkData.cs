using System.Collections.Generic;

namespace Common
{
    public partial class ChunkManager
    {
        public class BasicResource
        {
        }

        public class Chunk
        {
            public uint Id { get; set; }

            public uint Size { get; set; }

            public uint Offset { get; set; }

            public byte[] Data { get; set; }

            public BasicResource Resource { get; set; }

            public List<Chunk> SubChunks { get; set; }
        }
    }
}