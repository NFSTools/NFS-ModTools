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

            public long Offset { get; set; }

            public byte[] Data { get; set; }

            public bool HasPadding => Padding > 0;

            public uint Padding { get; set; }

            public uint PrePadding { get; set; }

            public BasicResource Resource { get; set; }

            public List<Chunk> SubChunks { get; set; }
        }
    }
}