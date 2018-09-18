using System.IO;

namespace VltEd.VLT
{
    public class VltChunk : IFileAccess
    {
        private long _offset;

        public VltChunkId ChunkId { get; set; }

        public int Length { get; set; }

        public int DataLength
        {
            get => Length - 0x8;
            set => Length = value + 0x8;
        }

        public bool IsValid => Length >= 0x8;

        public void GotoStart(Stream stream)
        {
            stream.Seek(_offset + 0x8, SeekOrigin.Begin);
        }

        public void SkipChunk(Stream stream)
        {
            stream.Seek(_offset + Length, SeekOrigin.Begin);
        }

        #region IFileAccess Members

        public void Read(BinaryReader br)
        {
            _offset = br.BaseStream.Position;
            ChunkId = (VltChunkId)br.ReadInt32();
            Length = br.ReadInt32();
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write((int)ChunkId);
            bw.Write(Length);
        }

        #endregion
    }
}
