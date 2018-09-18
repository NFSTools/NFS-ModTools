using System;
using System.IO;
using System.Text;

namespace VltEd.VLT
{
    public class VltPointerBlock : IFileAccess
    {
        public enum BlockType
        {
            Done = 0,
            RuntimeLink = 1,
            Switch = 2,
            Load = 3
        }

        private short _blockType;

        public int OffsetSource { get; set; }

        public BlockType Type
        {
            get => (BlockType)_blockType;
            set => _blockType = (short)value;
        }

        public short Identifier { get; set; }

        public int OffsetDest { get; set; }

        #region IFileAccess Members

        public void Read(BinaryReader br)
        {
            OffsetSource = br.ReadInt32();
            _blockType = br.ReadInt16();
            Identifier = br.ReadInt16();
            OffsetDest = br.ReadInt32();
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write(OffsetSource);
            bw.Write(_blockType);
            bw.Write(Identifier);
            bw.Write(OffsetDest);
        }

        #endregion

        public override string ToString()
        {
            var sb = new StringBuilder();
            switch ((BlockType)_blockType)
            {
                case BlockType.Done:
                    sb.Append("Done");
                    break;
                case BlockType.RuntimeLink:
                    sb.Append("RunL");
                    break;
                case BlockType.Load:
                    sb.Append("Load");
                    break;
                case BlockType.Switch:
                    sb.Append("SwiS");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            sb.AppendFormat("\tId={0}\tFrom={1:x}\tTo:{2:x}", Identifier, OffsetSource, OffsetDest);
            return sb.ToString();
        }
    }
}
