using System.IO;

namespace VltEd.VLT
{
    public abstract class VltBase : IFileAccess
    {
        public VltChunk Chunk { get; set; }

        public abstract void Read(BinaryReader br);
        public abstract void Write(BinaryWriter bw);
    }
}
