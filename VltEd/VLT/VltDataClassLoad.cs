using System.IO;

namespace VltEd.VLT
{
    public class VltDataClassLoad : VltDataBlock
    {
        private int _zero1, _zero2;

        public uint NameHash { get; private set; }

        public int CollectionCount { get; set; }

        public int Num2 { get; set; }

        public int TotalFieldsCount { get; set; }

        public int RequiredFieldsCount { get; set; }

        public int Pointer { get; private set; }

        public override void Read(BinaryReader br)
        {
            NameHash = br.ReadUInt32();
            CollectionCount = br.ReadInt32();
            TotalFieldsCount = br.ReadInt32();

            Pointer = (int)br.BaseStream.Position;
            br.ReadInt32();
            Num2 = br.ReadInt32();
            _zero1 = br.ReadInt32();
            RequiredFieldsCount = br.ReadInt32();
            _zero2 = br.ReadInt32();
        }

        public override void Write(BinaryWriter bw)
        {
            bw.Write(NameHash);
            bw.Write(CollectionCount);
            bw.Write(TotalFieldsCount);
            bw.Write(0xEFFECADD);
            bw.Write(Num2);
            bw.Write(_zero1);
            bw.Write(RequiredFieldsCount);
            bw.Write(_zero2);
        }
    }
}
