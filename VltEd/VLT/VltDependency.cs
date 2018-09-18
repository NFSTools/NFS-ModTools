using System.IO;

namespace VltEd.VLT
{
    public class VltDependency : VltBase
    {
        public const int VltFile = 0;
        public const int BinFile = 1;

        private int _count;
        private uint[] _hashes;
        private string[] _names;

        public uint GetHash(int index)
        {
            return _hashes[index];
        }

        public string GetName(int index)
        {
            return _names[index];
        }

        public override void Read(BinaryReader br)
        {
            _count = br.ReadInt32();
            _hashes = new uint[_count];
            _names = new string[_count];
            var offsets = new int[_count];
            for (var i = 0; i < _count; i++)
                _hashes[i] = br.ReadUInt32();
            for (var i = 0; i < _count; i++)
                offsets[i] = br.ReadInt32();

            var position = br.BaseStream.Position;
            for (var i = 0; i < _count; i++)
            {
                br.BaseStream.Seek(position + offsets[i], SeekOrigin.Begin);
                _names[i] = NullTerminatedString.Read(br);
            }
        }

        public override void Write(BinaryWriter bw)
        {
            var length = 0;
            bw.Write(_count);
            for (var i = 0; i < _count; i++)
                bw.Write(_hashes[i]);
            for (var i = 0; i < _count; i++)
            {
                bw.Write(length);
                length += _names[i].Length + 1;
            }
            for (var i = 0; i < _count; i++)
                NullTerminatedString.Write(bw, _names[i]);
        }
    }
}
