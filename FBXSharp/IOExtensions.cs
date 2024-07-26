using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace FBXSharp
{
    internal static class IOExtensions
    {
        public static int ReadTextInt32(this BinaryReader br)
        {
            var value = br.ReadByte();

            if (value == 0) return 0;

            var negative = value == '-';
            var result = negative ? 0 : value;

            while ((value = br.ReadByte()) >= '0' && value <= '9')
            {
                result *= 10;
                result += value - '0';
            }

            return negative ? -result : result;
        }

        public static uint ReadTextUInt32(this BinaryReader br)
        {
            uint result = 0;
            byte value;

            while ((value = br.ReadByte()) >= '0' && value <= '9')
            {
                result *= 10;
                result += (uint)(value - '0');
            }

            return result;
        }

        public static long ReadTextInt64(this BinaryReader br)
        {
            var value = br.ReadByte();

            if (value == 0) return 0;

            var negative = value == '-';
            var result = negative ? 0 : value;

            while ((value = br.ReadByte()) >= '0' && value <= '9')
            {
                result *= 10;
                result += value - '0';
            }

            return negative ? -result : result;
        }

        public static ulong ReadTextUInt64(this BinaryReader br)
        {
            ulong result = 0;
            byte value;

            while ((value = br.ReadByte()) >= '0' && value <= '9')
            {
                result *= 10;
                result += (ulong)(value - '0');
            }

            return result;
        }

        public static float ReadTextSingle(this BinaryReader br)
        {
            var value = br.ReadByte();

            if (value == 0) return 0.0f;

            var negative = value == '-';
            float result = negative ? 0 : value;

            while ((value = br.ReadByte()) >= '0' && value <= '9')
            {
                result *= 10;
                result += value - '0';
            }

            if (value == '.' || value == ',')
            {
                var exponent = 0.1f;

                while ((value = br.ReadByte()) >= '0' && value <= '9')
                {
                    result += (value - '0') * exponent;
                    exponent *= 0.1f;
                }

                if (value == 'e' || value == 'E')
                {
                    var power = br.ReadTextInt32();
                    result *= MathExtensions.FastSinglePow10(power);
                }
            }

            return negative ? -result : result;
        }

        public static double ReadTextDouble(this BinaryReader br)
        {
            var value = br.ReadByte();

            if (value == 0) return 0.0d;

            var negative = value == '-';
            double result = negative ? 0 : value;

            while ((value = br.ReadByte()) >= '0' && value <= '9')
            {
                result *= 10;
                result += value - '0';
            }

            if (value == '.' || value == ',')
            {
                var exponent = 0.1d;

                while ((value = br.ReadByte()) >= '0' && value <= '9')
                {
                    result += (value - '0') * exponent;
                    exponent *= 0.1d;
                }

                if (value == 'e' || value == 'E')
                {
                    var power = br.ReadTextInt32();
                    result *= MathExtensions.FastDoublePow10(power);
                }
            }

            return negative ? -result : result;
        }

        public static string ReadNullTerminated(this BinaryReader br)
        {
            var sb = new StringBuilder(0x100);
            byte value;

            while ((value = br.ReadByte()) != 0) _ = sb.Append((char)value);

            return sb.ToString();
        }

        public static string ReadNullTerminated(this BinaryReader br, int maxSize, bool alignToEnd = true)
        {
            var sb = new StringBuilder(maxSize);
            var current = br.BaseStream.Position;
            //byte value;

            for (var i = 0; i < maxSize /* && (value = br.ReadByte()) != 0 */; ++i)
                //_ = sb.Append((char)value);
                _ = sb.Append((char)br.ReadByte());

            if (alignToEnd) br.BaseStream.Position = current + maxSize;

            return sb.ToString();
        }

        public static void WriteStringPrefixByte(this BinaryWriter bw, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                bw.Write((byte)0);
                return;
            }

            bw.Write((byte)value.Length);

            for (var i = 0; i < value.Length; ++i) bw.Write((byte)value[i]);
        }

        public static void WriteStringPrefixInt(this BinaryWriter bw, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                bw.Write(0);
                return;
            }

            bw.Write(value.Length);

            for (var i = 0; i < value.Length; ++i) bw.Write((byte)value[i]);
        }

        public static void WriteBytesPrefixed(this BinaryWriter bw, byte[] value)
        {
            bw.Write(value.Length);
            bw.Write(value);
        }

        public static unsafe T ReadUnmanaged<T>(this BinaryReader br) where T : unmanaged
        {
            var array = new byte[sizeof(T)];
            _ = br.BaseStream.Read(array, 0, array.Length);
            fixed (byte* ptr = &array[0])
            {
                return *(T*)ptr;
            }
        }

        public static T ReadManaged<T>(this BinaryReader br) where T : struct
        {
            var size = Marshal.SizeOf<T>();
            var array = br.ReadBytes(size);

            var handle = GCHandle.Alloc(array, GCHandleType.Pinned);
            var result = Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
            handle.Free();

            return result;
        }

        public static unsafe void WriteUnmanaged<T>(this BinaryWriter bw, T value) where T : unmanaged
        {
            var array = new byte[sizeof(T)];
            fixed (byte* ptr = &array[0])
            {
                *(T*)ptr = value;
            }

            bw.BaseStream.Write(array, 0, array.Length);
        }

        public static void WriteManaged<T>(this BinaryWriter bw, T value) where T : struct
        {
            var size = Marshal.SizeOf<T>();
            var array = new byte[size];

            var handle = GCHandle.Alloc(array, GCHandleType.Pinned);
            Marshal.StructureToPtr(value, handle.AddrOfPinnedObject(), false);

            bw.Write(array);
            handle.Free();
        }

        public static void FillBuffer(this BinaryWriter bw, int align)
        {
            var padding = align - (int)(bw.BaseStream.Position % align);

            if (padding == align) padding = 0;

            for (var i = 0; i < padding; ++i) bw.Write((byte)0);
        }
    }
}