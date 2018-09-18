using System;
using System.IO;

namespace VltEd.VLT.MW
{
    public class MostWantedExpressionBlock : ExpressionBlockBase
    {
        public override void ReadData(BinaryReader br)
        {
            br.BaseStream.Position = Offset;

            switch (ExpressionType)
            {
                case VltExpressionType.CollectionLoadData:
                {
                    Data = new MostWantedCollectionLoad();
                    break;
                }
                default: throw new Exception($"Unsupported: {ExpressionType}");
            }

            Data.Address = Offset;
            Data.Expression = this;
            Data.Read(br);
        }

        public override void WriteData(BinaryWriter bw)
        {
            bw.BaseStream.Position = Offset;
            Data.Write(bw);
        }

        public override void Read(BinaryReader br)
        {
            Id = br.ReadUInt32();
            ExpressionType = (VltExpressionType) br.ReadUInt32();
            br.ReadInt32();
            Length = br.ReadInt32();
            Offset = br.ReadInt32();
        }

        public override void Write(BinaryWriter bw)
        {
            throw new NotImplementedException();
        }
    }
}
