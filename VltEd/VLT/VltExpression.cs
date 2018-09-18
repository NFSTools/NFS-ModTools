using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace VltEd.VLT
{
    public abstract class VltExpression : VltBase, IEnumerable<ExpressionBlockBase>
    {
        protected List<ExpressionBlockBase> Blocks;

        public ExpressionBlockBase this[int index]
        {
            get => Blocks[index];
            set => Blocks[index] = value;
        }

        public override void Read(BinaryReader br)
        {
            var numBlocks = br.ReadInt32();

            Blocks = new List<ExpressionBlockBase>(numBlocks);
        }

        public override void Write(BinaryWriter bw)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<ExpressionBlockBase> GetEnumerator()
        {
            return Blocks.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
