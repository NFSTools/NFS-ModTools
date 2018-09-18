using System.IO;

namespace VltEd.VLT
{
    public abstract class ExpressionBlockBase : IFileAccess
    {
        public uint Id { get; set; }

        public VltExpressionType ExpressionType { get; set; }

        public int Length { get; set; }

        public int Offset { get; set; }

        public VltDataBlock Data { get; set; }

        public abstract void ReadData(BinaryReader br);

        public abstract void WriteData(BinaryWriter bw);

        public abstract void Read(BinaryReader br);

        public abstract void Write(BinaryWriter bw);
    }
}
