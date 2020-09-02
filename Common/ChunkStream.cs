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

        public byte Length => (byte) _length;

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
        public bool IsParent => ((int)Type & 0x80000000) != 0;

        public uint Type { get; set; }

        public int EndOffset
        {
            get => Offset + Length + 0x8;
            set => Length = value - Offset - 0x8;
        }

        public int Offset { get; set; }

        public int Length { get; set; }

        public int RawLength => Length + 0x8;

        public void Read(BinaryReader br)
        {
            Offset = (int)br.BaseStream.Position;
            Type = br.ReadUInt32();
            Length = br.ReadInt32();
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
        private readonly SysStream _stream;
        private readonly Stack<RealChunk> _chunkStack;
        private readonly BinaryWriter _binaryWriter;
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

        public void NextAlignmentAuto(bool byteVal = false)
        {
            var alignment = 0x10;

            if (_stream.Position % alignment != 0)
            {
                var nb = alignment - _stream.Position % alignment;

                if (byteVal)
                {
                    for (var i = 0; i < nb; i++)
                    {
                        _stream.WriteByte(0x11);
                    }
                }
                else
                {
                    _stream.Position += nb;
                }
            }
        }

        public void NextAlignment(int alignment, bool byteVal = false)
        {
            if (_stream.Position % alignment != 0)
            {
                var nb = alignment - _stream.Position % alignment;

                if (byteVal)
                {
                    for (var i = 0; i < nb; i++)
                    {
                        _stream.WriteByte(0x11);
                    }
                }
                else
                {
                    _stream.Position += nb;
                }
            }
        }

        public int PaddingAlignment(int padding)
        {
            if (_stream.Position % padding == 0) return 0;

            BeginChunk(0x00000000);

            var offset = 0;

            if (_stream.Position % padding != 0)
            {
                offset = (int)(padding - _stream.Position % padding);
                _stream.Seek(offset, SeekOrigin.Current);
            }

            EndChunk();

            return offset + 8;
        }

        public int PaddingAlignment2(int padding)
        {
            BeginChunk(0x00000000);

            var result = Math.Max(0, Math.Min(padding - _stream.Position % padding - 8, 0x8000));

            if (result < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(padding));
            }

            var offset = (int) result;
            _stream.Seek(offset, SeekOrigin.Current);

            EndChunk();

            return offset + 8;
        }

        public RealChunk BeginChunk(uint type)
        {
            var chunk = new RealChunk
            {
                Offset = (int)_stream.Position,
                Type = type
            };
            _chunkStack.Push(chunk);
            _stream.Seek(0x8, SeekOrigin.Current);
            return chunk;
        }

        public void EndChunk()
        {
            var chunk = _chunkStack.Pop();
            chunk.EndOffset = (int)_stream.Position;
            chunk.Write(_binaryWriter);
        }

        public void WriteStruct<T>(T instance)
        {
            if (!_canWrite)
                throw new InvalidOperationException("Cannot write to this stream");
            _binaryWriter.PutStruct(instance);
        }

        public void Write(FixedLenString str)
        {
            if (!_canWrite)
                throw new InvalidOperationException("Cannot write to this stream");
            str.Write(_binaryWriter);
        }

        public void Write(byte[] data)
        {
            if (!_canWrite)
                throw new InvalidOperationException("Cannot write to this stream");
            _binaryWriter.Write(data);
        }

        public void Write(ushort[] data)
        {
            if (!_canWrite)
                throw new InvalidOperationException("Cannot write to this stream");
            foreach (var value in data)
            {
                _binaryWriter.Write(value);
            }
        }

        public void Write(short[] data)
        {
            if (!_canWrite)
                throw new InvalidOperationException("Cannot write to this stream");
            foreach (var value in data)
            {
                _binaryWriter.Write(value);
            }
        }

        public void Write(uint[] data)
        {
            if (!_canWrite)
                throw new InvalidOperationException("Cannot write to this stream");
            foreach (var value in data)
            {
                _binaryWriter.Write(value);
            }
        }

        public void Write(int[] data)
        {
            if (!_canWrite)
                throw new InvalidOperationException("Cannot write to this stream");
            foreach (var value in data)
            {
                _binaryWriter.Write(value);
            }
        }

        public void Write(float[] data)
        {
            if (!_canWrite)
                throw new InvalidOperationException("Cannot write to this stream");
            foreach (var value in data)
            {
                _binaryWriter.Write(value);
            }
        }

        public void Write(sbyte data)
        {
            if (!_canWrite)
                throw new InvalidOperationException("Cannot write to this stream");
            _binaryWriter.Write(data);
        }

        public void Write(byte data)
        {
            if (!_canWrite)
                throw new InvalidOperationException("Cannot write to this stream");
            _binaryWriter.Write(data);
        }

        public void Write(short data)
        {
            if (!_canWrite)
                throw new InvalidOperationException("Cannot write to this stream");
            _binaryWriter.Write(data);
        }

        public void Write(ushort data)
        {
            if (!_canWrite)
                throw new InvalidOperationException("Cannot write to this stream");
            _binaryWriter.Write(data);
        }

        public void Write(int data)
        {
            if (!_canWrite)
                throw new InvalidOperationException("Cannot write to this stream");
            _binaryWriter.Write(data);
        }

        public void Write(uint data)
        {
            if (!_canWrite)
                throw new InvalidOperationException("Cannot write to this stream");
            _binaryWriter.Write(data);
        }

        public void Write(float data)
        {
            if (!_canWrite)
                throw new InvalidOperationException("Cannot write to this stream");
            _binaryWriter.Write(data);
        }

        public void WriteChunk(ChunkManager.Chunk chunk)
        {
            BeginChunk(chunk.Id);

            if (chunk.HasPadding)
            {
                NextAlignmentAuto(true);
            }

            if ((chunk.Id & 0x80000000) == 0x80000000)
            {
                WriteChunkChildren(chunk.SubChunks);
            }
            else
            {
                Write(chunk.Data);
            }

            EndChunk();
        }

        private void WriteChunkChildren(List<ChunkManager.Chunk> chunks)
        {
            foreach (var chunk in chunks)
            {
                WriteChunk(chunk);
            }
        }

        public SysStream GetStream()
        {
            return _stream;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _stream?.Dispose();

            if (!_canWrite) return;

            _binaryWriter?.Dispose();
            _canWrite = false;
        }
    }
}
