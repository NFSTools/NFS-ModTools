using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Common
{
    public static class BinaryUtil
    {
        /// <summary>
        /// Search for a chunk, return its size, and put its offset into a variable.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="chunkId"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static uint FindChunk(BinaryReader reader, uint chunkId, ref long offset)
        {
            uint readSize = 0;

            while (reader.BaseStream.Position != reader.BaseStream.Length)
            {
                var readMagic = reader.ReadUInt32();
                readSize = reader.ReadUInt32();

                if (readSize != 0 && readMagic == chunkId)
                {
                    offset = reader.BaseStream.Position - 8;

                    reader.BaseStream.Position = 0;

                    return readSize;
                }

                if (readSize != 0)
                    reader.BaseStream.Seek(readSize, SeekOrigin.Current);
            }

            reader.BaseStream.Position = 0;

            return readSize;
        }

        public static uint AlignReader(BinaryReader br, int alignTo)
        {
            if (br.BaseStream.Position % alignTo == 0)
            {
                return 0;
            }

            var align = alignTo - br.BaseStream.Position % alignTo;
            br.BaseStream.Position += align;
            return (uint)align;
        }

        /// <summary>
        /// Read a C-style string from a binary file.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static string ReadNullTerminatedString(BinaryReader stream)
        {
            var str = new StringBuilder();
            char ch;
            while ((ch = (char)stream.ReadByte()) != 0)
                str.Append(ch);
            return str.ToString();
        }

        /// <summary>
        /// Read a structure from a binary file.
        /// </summary>
        /// <param name="reader"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T ReadStruct<T>(BinaryReader reader) where T : struct
        {
            var structSize = Marshal.SizeOf<T>();
            var bytes = reader.ReadBytes(structSize);

            Debug.Assert(bytes.Length == structSize, "bytes.Length == structSize");

            unsafe
            {
                fixed (byte* bp = &bytes[0])
                {
                    return Marshal.PtrToStructure<T>(new IntPtr(bp));
                }
            }
        }
        
        /// <summary>
        /// Read an unmanaged structure from a binary file.
        /// </summary>
        /// <param name="reader"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T ReadUnmanagedStruct<T>(BinaryReader reader) where T : unmanaged
        {
            var structSize = Unsafe.SizeOf<T>();
            var bytes = reader.ReadBytes(structSize);

            Debug.Assert(bytes.Length == structSize, "bytes.Length == structSize");

            return MemoryMarshal.Read<T>(bytes);
        }

        /// <summary>
        /// BinaryReader extension method for reading raw structures.
        /// </summary>
        /// <param name="reader"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetStruct<T>(this BinaryReader reader) where T : struct
        {
            return ReadStruct<T>(reader);
        }

        public static Vector2 ReadVector2(BinaryReader binaryReader)
        {
            return ReadUnmanagedStruct<Vector2>(binaryReader);
        }

        public static Vector3 ReadVector3(BinaryReader binaryReader)
        {
            return ReadUnmanagedStruct<Vector3>(binaryReader);
        }

        public static Vector2 ReadShort2N(BinaryReader binaryReader)
        {
            var packed = binaryReader.ReadInt32();
            var x = (short)packed / 32767f;
            var y = (short)(packed >> 16) / 32767f;

            return new Vector2(x, y);
        }

        public static Vector4 ReadShort4N(BinaryReader binaryReader)
        {
            var packed = binaryReader.ReadInt64();
            var x = (short)packed / 32767f;
            var y = (short)(packed >> 16) / 32767f;
            var z = (short)(packed >> 32) / 32767f;
            var w = (short)(packed >> 48) / 32767f;
            return new Vector4(x, y, z, w);
        }
        
        public static Vector3 ReadNormal(BinaryReader binaryReader, bool packed = false)
        {
            var vec = packed ? ReadShort4N(binaryReader) : new Vector4(ReadVector3(binaryReader), 1);
            return new Vector3(vec.X, vec.Y, vec.Z);
        }

        public static void PutStruct<T>(this BinaryWriter writer, T instance) where T : struct
        {
            unsafe
            {
                var byteArray = new byte[Marshal.SizeOf<T>()];
                fixed (byte* byteArrayPtr = byteArray)
                {
                    Marshal.StructureToPtr(instance, (IntPtr)byteArrayPtr, true);
                }
                writer.Write(byteArray);
            }
        }
    }
}