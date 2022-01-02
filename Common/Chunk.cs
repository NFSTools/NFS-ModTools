using System.Collections.Generic;

namespace Common;

public class Chunk
{
    public uint Id { get; set; }

    public uint Size { get; set; }

    public long Offset { get; set; }

    public byte[] Data { get; set; }
    public BasicResource Resource { get; set; }

    public List<Chunk> SubChunks { get; set; }
}