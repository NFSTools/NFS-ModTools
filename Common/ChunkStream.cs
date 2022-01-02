using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SysStream = System.IO.Stream;

namespace Common
{
    // Code in this file is based on Arushan's Most Wanted Geometry Compiler. 

    public struct FixedLenString
    {
        private int _length;
        private string _string;

        public FixedLenString(string data)
        {
            _length = data.Length + 4 - data.Length % 4;
            _string = data;
        }

        public FixedLenString(string data, int length)
        {
            _length = length;
            _string = data;
        }

        public FixedLenString(BinaryReader br, int length)
        {
            _length = 0;
            _string = "";
            Read(br, length);
        }

        public FixedLenString(BinaryReader br)
        {
            _length = 0;
            _string = "";
            Read(br);
        }

        public byte Length => (byte)_length;

        public void Read(BinaryReader br, int length)
        {
            _length = length;
            var bytes = br.ReadBytes(length);
            var data = Encoding.ASCII.GetString(bytes);
            _string = data.TrimEnd((char)0);
        }

        public void Read(BinaryReader br)
        {
            _length = 0;
            _string = "";
            while (true)
            {
                var bytes = br.ReadBytes(4);
                var data = Encoding.ASCII.GetString(bytes);
                _string += data;
                _length += 4;
                if (data.IndexOf((char)0) < 0) continue;
                _string = _string.TrimEnd((char)0);
                break;
            }
        }

        public void Write(BinaryWriter bw)
        {
            var data = _string.PadRight(_length, (char)0);
            var bytes = Encoding.ASCII.GetBytes(data);
            bw.Write(bytes);
        }

        public override string ToString()
        {
            return _string;
        }
    }

    public class RealChunk
    {
        public bool IsParent => (Type & 0x80000000) != 0;

        public uint Type { get; set; }

        public long EndOffset
        {
            get => Offset + Length + 0x8;
            set => Length = (uint)(value - Offset - 0x8);
        }

        public long Offset { get; set; }

        public uint Length { get; set; }

        public void Read(BinaryReader br)
        {
            Offset = (int)br.BaseStream.Position;
            Type = br.ReadUInt32();
            Length = br.ReadUInt32();
        }

        public void Write(BinaryWriter bw)
        {
            var offset = bw.BaseStream.Position;
            bw.BaseStream.Seek(Offset, SeekOrigin.Begin);
            bw.Write(Type);
            bw.Write(Length);
            bw.BaseStream.Seek(offset, SeekOrigin.Begin);
        }

        public void GoToStart(SysStream fs)
        {
            fs.Seek(Offset + 8, SeekOrigin.Begin);
        }

        public void Skip(SysStream fs)
        {
            fs.Seek(EndOffset, SeekOrigin.Begin);
        }

        public override string ToString()
        {
            return "RealChunk {\n\tType=" + $"{Type:X}"
                                          + ",\n\tOffset=" + $"{Offset:X}"
                                          + ",\n\tLength=" + $"{Length:X}"
                                          + "\n}";
        }
    }

    public class ChunkStream : IDisposable
    {
        private readonly BinaryWriter _binaryWriter;
        private readonly Stack<RealChunk> _chunkStack;
        private readonly SysStream _stream;
        private bool _canWrite;

        public ChunkStream(SysStream stream)
        {
            _stream = stream;
            _chunkStack = new Stack<RealChunk>();
            _binaryWriter = new BinaryWriter(_stream);
            _canWrite = true;
        }

        public ChunkStream(BinaryReader binaryReader)
        {
            _stream = binaryReader.BaseStream;
            _chunkStack = new Stack<RealChunk>();
        }

        public ChunkStream(BinaryWriter binaryWriter)
        {
            _stream = binaryWriter.BaseStream;
            _chunkStack = new Stack<RealChunk>();
            _binaryWriter = binaryWriter;
            _canWrite = true;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _stream?.Dispose();

            if (!_canWrite) return;

            _binaryWriter?.Dispose();
            _canWrite = false;
        }

        public SysStream GetStream()
        {
            return _stream;
        }
    }
}