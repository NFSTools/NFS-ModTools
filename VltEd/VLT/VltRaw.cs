using System.IO;

namespace VltEd.VLT
{
    public class VltRaw : VltBase
    {
        public byte[] Data { get; set; }

        public Stream GetStream()
        {
            return new MemoryStream(Data);
        }

        public override void Read(BinaryReader br)
        {
            Data = br.ReadBytes(Chunk.DataLength);
        }

        public override void Write(BinaryWriter bw)
        {
            bw.Write(Data);
        }
    }
}
