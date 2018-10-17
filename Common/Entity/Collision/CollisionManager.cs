using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Entity.Data;

namespace Common.Entity.Collision
{
    public abstract class CollisionManager
    {
        public abstract CollisionPack ReadCollisionPack(BinaryReader br);
    }
}
