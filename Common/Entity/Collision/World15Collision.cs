using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Common.Entity.Data;

namespace Common.Entity.Collision
{
    public class World15Collision : CollisionManager
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct CollisionPackHeader
        {
            public uint DataSize; // chunk size - (padding? + 16)
            public uint PackID; // written as string later... wtf is this?
            public long Blank;
        }

        public override CollisionPack ReadCollisionPack(BinaryReader br)
        {
            var padding = 0u;

            while (br.ReadByte() == 0x11)
            {
                padding++;
            }

            br.BaseStream.Position--;

            if (padding % 2 != 0 || br.BaseStream.Position % 2 != 0)
            {
                br.BaseStream.Position--;
            }

            var header = br.GetStruct<CollisionPackHeader>();

            // "PRAC" -> "CARP"
            if (br.ReadUInt32() != 0x43415250)
            {
                throw new Exception("CollPack FourCC is not CARP!");
            }

            br.ReadUInt32();
            br.ReadUInt64();

            // "itrA" -> "Arti"
            if (br.ReadUInt32() != 0x41727469)
            {
                throw new Exception("CollPack sub-FourCC #1 is not Arti!");
            }

            br.ReadUInt32(); // usually == 0x12?

            var numEntries = br.ReadUInt32() - 1;

            br.ReadUInt32(); // usually == 0x01?

            // "emaN" -> "Name"
            if (br.ReadUInt32() != 0x4e616d65)
            {
                throw new Exception("CollPack sub-FourCC #2 is not Name!");
            }

            br.ReadUInt64();

            var rows = new List<byte[]>();

            for (var i = 0; i < numEntries; i++)
            {
                rows.Add(br.ReadBytesRequired(0x10));
            }

            return new CollisionPack();
        }
    }
}
