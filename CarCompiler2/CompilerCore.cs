using System.IO;
using System.Text;

namespace CarCompiler
{
    public enum GeometryChunks : uint
    {
        Null = 0,

        Geometry = 0x80134000,
        // {
        GeometryParts = 0x80134001,
        // {
        GeometryPartsDesc = 0x00134002,
        GeometryPartsHash = 0x00134003,
        GeometryPartsOffset = 0x00134004,
        GeometryPartsEmpty = 0x80134008,
        // }
        GeometryPart = 0x80134010,
        // {
        GeometryPartDesc = 0x00134011,
        GeometryPartTextures = 0x00134012,
        GeometryPartShaders = 0x00134013,
        // 0x0013401A {hash, 0, 0, 0, d3dmatrix}
        GeometryPartMountPoints = 0x0013401A,
        GeometryPartData = 0x80134100,
        // {
        GeometryPartDataDesc = 0x00134900,
        GeometryPartDataVertices = 0x00134B01,
        GeometryPartDataGroups = 0x00134B02,
        GeometryPartDataIndices = 0x00134B03,
        GeometryPartDataMaterialName = 0x00134C02
        // ?? 0x00134017
        // }
        // ?? 0x00134017
        // ?? 0x00134018
        // ?? 0x00134019
        // }
        // }
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

        public void GoToStart(Stream fs)
        {
            fs.Seek(Offset + 8, SeekOrigin.Begin);
        }

        public void Skip(Stream fs)
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
                if (data.IndexOf((char)0) >= 0)
                {
                    _string = _string.TrimEnd((char)0);
                    break;
                }
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

    public struct RealVector2
    {
        public float u, v;
        public RealVector2(float u, float v)
        {
            this.u = u;
            this.v = v;
        }
        public void Read(BinaryReader reader)
        {
            u = reader.ReadSingle();
            v = reader.ReadSingle();
        }
        public void Write(BinaryWriter writer)
        {
            writer.Write(u);
            writer.Write(v);
        }
        public override string ToString()
        {
            return "Vector2 {" + u + "," + v + "}";
        }
        public override int GetHashCode()
        {
            var hash = u.GetHashCode() + ":" + v.GetHashCode();
            return hash.GetHashCode();
        }
    }

    public struct RealVector3
    {
        public float x, y, z;
        public RealVector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
        public void Read(BinaryReader reader)
        {
            x = reader.ReadSingle();
            y = reader.ReadSingle();
            z = reader.ReadSingle();
        }
        public void Write(BinaryWriter writer)
        {
            writer.Write(x);
            writer.Write(y);
            writer.Write(z);
        }
        public override string ToString()
        {
            return "Vector3 {" + x + "," + y + "," + z + "}";
        }
        public override int GetHashCode()
        {
            var hash = x.GetHashCode() + ":" + y.GetHashCode() + ":" + z.GetHashCode();
            return hash.GetHashCode();
        }
    }

    public struct RealVector4
    {
        public float x, y, z, w;
        public RealVector4(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }
        public RealVector4(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = 0.0f;
        }
        public void Read(BinaryReader reader)
        {
            x = reader.ReadSingle();
            y = reader.ReadSingle();
            z = reader.ReadSingle();
            w = reader.ReadSingle();
        }
        public void Write(BinaryWriter writer)
        {
            writer.Write(x);
            writer.Write(y);
            writer.Write(z);
            writer.Write(w);
        }
        public override string ToString()
        {
            return "Vector4 {" + x + "," + y + "," + z + "," + w + "}";
        }

    }

    public struct RealMatrix
    {
        public float[] m;
        public float Get(int i, int j)
        {
            return m[i * 4 + j];
        }
        public void Set(int i, int j, float newValue)
        {
            m[i * 4 + j] = newValue;
        }
        public void Read(BinaryReader reader)
        {
            m = new float[16];
            for (var i = 0; i < 16; i++)
                m[i] = reader.ReadSingle();
        }
        public void Write(BinaryWriter writer)
        {
            for (var i = 0; i < 16; i++)
                writer.Write(m[i]);
        }

    }
}
