using System.Diagnostics;
using System.IO;

namespace VltEd.VLT.MW
{
    public class MostWantedCollectionLoad : CollectionLoadBase
    {
        public override void Read(BinaryReader br)
        {
            NameHash = br.ReadUInt32();
            ClassNameHash = br.ReadUInt32();
            ParentHash = br.ReadUInt32();
            CountOpt1 = br.ReadInt32();
            Num1 = br.ReadInt32();         // not always 0, 0xc in one case
            CountOpt2 = br.ReadInt32();

            Debug.Assert(CountOpt1 == CountOpt2, "CountOpt1 not equal to CountOpt2");

            CountTypes = br.ReadInt32();

            Pointer = (int)br.BaseStream.Position;
            br.ReadInt32();
            TypeHashes = new uint[CountTypes];
            for (var i = 0; i < CountTypes; i++)
            {
                TypeHashes[i] = br.ReadUInt32();
            }

            OptData = new OptionalData[CountOpt1];
            for (var i = 0; i < CountOpt1; i++)
            {
                OptData[i] = new OptionalData();
                OptData[i].Read(br);
            }
        }

        public override void Write(BinaryWriter bw)
        {
            throw new System.NotImplementedException();
        }
    }
}
