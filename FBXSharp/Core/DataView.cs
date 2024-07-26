using System;
using System.IO;

namespace FBXSharp.Core
{
    public struct DataView
    {
        public long Start { get; }
        public long End { get; }
        public bool IsBinary { get; }
        public BinaryReader Reader { get; }

        public DataView(long start, long end, bool isBinary, BinaryReader reader)
        {
            Start = start;
            End = end;
            IsBinary = isBinary;
            Reader = reader;
        }

        public override bool Equals(object obj)
        {
            return obj is DataView dataView && dataView == this;
        }

        public override int GetHashCode()
        {
            return Tuple.Create(Start, End, IsBinary, Reader).GetHashCode();
        }

        public static bool operator ==(DataView dataView1, DataView dataView2)
        {
            return dataView1.Start == dataView2.Start && dataView1.End == dataView2.End &&
                   dataView1.IsBinary == dataView2.IsBinary && dataView1.Reader == dataView2.Reader;
        }

        public static bool operator !=(DataView dataView1, DataView dataView2)
        {
            return !(dataView1 == dataView2);
        }

        public int ToInt32(int offset = 0)
        {
            if (Reader is null) return 0;

            var current = Reader.BaseStream.Position;
            Reader.BaseStream.Position = Start + offset;

            if (IsBinary)
            {
                var result = Reader.ReadInt32();
                Reader.BaseStream.Position = current;
                return result;
            }
            else
            {
                var result = Reader.ReadTextInt32();
                Reader.BaseStream.Position = current;
                return result;
            }
        }

        public uint ToUInt32(int offset = 0)
        {
            if (Reader is null) return 0;

            var current = Reader.BaseStream.Position;
            Reader.BaseStream.Position = Start + offset;

            if (IsBinary)
            {
                var result = Reader.ReadUInt32();
                Reader.BaseStream.Position = current;
                return result;
            }
            else
            {
                var result = Reader.ReadTextUInt32();
                Reader.BaseStream.Position = current;
                return result;
            }
        }

        public long ToInt64(int offset = 0)
        {
            if (Reader is null) return 0;

            var current = Reader.BaseStream.Position;
            Reader.BaseStream.Position = Start + offset;

            if (IsBinary)
            {
                var result = Reader.ReadInt64();
                Reader.BaseStream.Position = current;
                return result;
            }
            else
            {
                var result = Reader.ReadTextInt64();
                Reader.BaseStream.Position = current;
                return result;
            }
        }

        public ulong ToUInt64(int offset = 0)
        {
            if (Reader is null) return 0;

            var current = Reader.BaseStream.Position;
            Reader.BaseStream.Position = Start + offset;

            if (IsBinary)
            {
                var result = Reader.ReadUInt64();
                Reader.BaseStream.Position = current;
                return result;
            }
            else
            {
                var result = Reader.ReadTextUInt64();
                Reader.BaseStream.Position = current;
                return result;
            }
        }

        public float ToSingle(int offset = 0)
        {
            if (Reader is null) return 0;

            var current = Reader.BaseStream.Position;
            Reader.BaseStream.Position = Start + offset;

            if (IsBinary)
            {
                var result = Reader.ReadSingle();
                Reader.BaseStream.Position = current;
                return result;
            }
            else
            {
                var result = Reader.ReadTextSingle();
                Reader.BaseStream.Position = current;
                return result;
            }
        }

        public double ToDouble(int offset = 0)
        {
            if (Reader is null) return 0;

            var current = Reader.BaseStream.Position;
            Reader.BaseStream.Position = Start + offset;

            if (IsBinary)
            {
                var result = Reader.ReadDouble();
                Reader.BaseStream.Position = current;
                return result;
            }
            else
            {
                var result = Reader.ReadTextDouble();
                Reader.BaseStream.Position = current;
                return result;
            }
        }

        public override string ToString()
        {
            if (Reader is null) return string.Empty;

            var current = Reader.BaseStream.Position;
            Reader.BaseStream.Position = Start;
            var size = (int)(End - Start);

            var result = Reader.ReadNullTerminated(size);
            Reader.BaseStream.Position = current;
            return result;
        }
    }
}