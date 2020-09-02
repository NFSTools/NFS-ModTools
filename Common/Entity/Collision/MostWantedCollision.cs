using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Entity.Data;

namespace Common.Entity.Collision
{
    public class MostWantedCollision : CollisionManager
    {
        public override CollisionPack ReadCollisionPack(BinaryReader br)
        {
            BinaryUtil.SkipPadding(br);

            var unknownSize = br.ReadUInt32(); // chunk size - 0x18?
            var unknownId = br.ReadUInt32(); // seen later in text form

            br.BaseStream.Position += 8;

            return new CollisionPack();
        }
    }
}
