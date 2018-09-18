using System.IO;

namespace VltEd.VLT
{
    public class VltDataDatabaseLoad : VltDataBlock
    {
        private int[] _sizes;

        public int this[int index]
        {
            get => _sizes[index];
            set => _sizes[index] = value;
        }

        public int Count { get; private set; }

        public int Num1 { get; set; }

        public int Num2 { get; set; }

        /// <summary>
        /// Gets the pointer to a string array of types
        /// </summary>
        public int Pointer { get; private set; }

        public override void Read(BinaryReader br)
        {
            Num1 = br.ReadInt32();
            Num2 = br.ReadInt32();
            Count = br.ReadInt32();
            Pointer = (int) br.BaseStream.Position;
            br.ReadInt32();
            _sizes = new int[Count];
            for (var i = 0; i < Count; i++)
            {
                _sizes[i] = br.ReadInt32();
            }
        }

        public override void Write(BinaryWriter bw)
        {
            bw.Write(Num1);
            bw.Write(Num2);
            bw.Write(Count);
            bw.Write(0xEFFECADD);
            for (var i = 0; i < Count; i++)
            {
                bw.Write(_sizes[i]);
            }
        }
    }
}
