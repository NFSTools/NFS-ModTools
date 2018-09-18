using System.IO;

namespace VltEd.VLT.MW
{
    public class MostWantedExpression : VltExpression
    {
        public override void Read(BinaryReader br)
        {
            base.Read(br);

            for (var i = 0; i < Blocks.Capacity; i++)
            {
                Blocks[i] = new MostWantedExpressionBlock();
                Blocks[i].Read(br);
            }
        }
    }
}
