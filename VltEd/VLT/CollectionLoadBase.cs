using System.IO;

namespace VltEd.VLT
{
    public abstract class CollectionLoadBase : VltDataBlock
    {
        public class OptionalData : IFileAccess
        {
            public uint NameHash { get; private set; }

            public int Pointer { get; private set; }

            public short Flags1 { get; private set; }

            public short Flags2 { get; private set; }

            public bool IsDataEmbedded => (Flags2 & 0x20) != 0;

            #region IFileAccess Members

            public void Read(BinaryReader br)
            {
                NameHash = br.ReadUInt32();
                Pointer = (int)br.BaseStream.Position;
                br.ReadInt32();
                Flags1 = br.ReadInt16();
                Flags2 = br.ReadInt16();
            }

            public void Write(BinaryWriter bw)
            {
                bw.Write(NameHash);
                bw.Write(0xEFFECADD);   // beware, sometimes this is a ptr, sometimes this is data
                // recommend writing collload block before writing data
                bw.Write(Flags1);
                bw.Write(Flags2);
            }

            #endregion
        }

        protected int CountOpt1;
        protected int CountOpt2, CountTypes;
        protected uint[] TypeHashes;
        protected OptionalData[] OptData;

        public uint NameHash { get; set; }

        public uint ClassNameHash { get; set; }

        public uint ParentHash { get; set; }

        public int Count => CountTypes;

        public int CountOptional => CountOpt1;

        public int Pointer { get; set; }

        public OptionalData this[int index] => OptData[index];

        public int Num1 { get; set; }
    }
}
