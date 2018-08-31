using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Common
{
    public static class BinaryUtil
    {
        public const string DoubleFixedPoint = "0.###################################################################################################################################################################################################################################################################################################################################################";

        // Note this MODIFIES THE GIVEN ARRAY then returns a reference to the modified array.
        public static byte[] Reverse(this byte[] b)
        {
            Array.Reverse(b);
            return b;
        }

        public static UInt16 ReadUInt16BE(this BinaryReader binRdr)
        {
            return BitConverter.ToUInt16(binRdr.ReadBytesRequired(sizeof(UInt16)).Reverse(), 0);
        }

        public static Int16 ReadInt16BE(this BinaryReader binRdr)
        {
            return BitConverter.ToInt16(binRdr.ReadBytesRequired(sizeof(Int16)).Reverse(), 0);
        }

        public static UInt32 ReadUInt32BE(this BinaryReader binRdr)
        {
            return BitConverter.ToUInt32(binRdr.ReadBytesRequired(sizeof(UInt32)).Reverse(), 0);
        }

        public static Int32 ReadInt32BE(this BinaryReader binRdr)
        {
            return BitConverter.ToInt32(binRdr.ReadBytesRequired(sizeof(Int32)).Reverse(), 0);
        }

        public static byte[] ReadBytesRequired(this BinaryReader binRdr, int byteCount)
        {
            var result = binRdr.ReadBytes(byteCount);

            if (result.Length != byteCount)
                throw new EndOfStreamException(string.Format("{0} bytes required from stream, but only {1} returned.", byteCount, result.Length));

            return result;
        }

        public static string FullPrecisionFloat(float f)
        {
            return f.ToString(DoubleFixedPoint);
        }

        /// <summary>
        /// Calculate the number of bytes necessary for alignment to an offset.
        /// </summary>
        /// <param name="num"></param>
        /// <param name="alignTo"></param>
        /// <returns></returns>
        public static long PaddingAlign(long num, int alignTo)
        {
            if (num % alignTo == 0)
            {
                return 0;
            }

            return alignTo - num % alignTo;
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
        public static T ReadStruct<T>(BinaryReader reader)
        {
            var bytes = reader.ReadBytes(Marshal.SizeOf(typeof(T)));

            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            var theStructure = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            handle.Free();

            return theStructure;
        }

        /// <summary>
        /// BinaryReader extension method for reading raw structures.
        /// </summary>
        /// <param name="reader"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetStruct<T>(this BinaryReader reader)
        {
            return ReadStruct<T>(reader);
        }

        /// <summary>
        /// BinaryWriter extension method for writing raw structures.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="value"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static void PutStruct<T>(this BinaryWriter reader, T value)
        {
            WriteStruct(reader, value);
        }

        /// <summary>
        /// Write a structure to a binary file.
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="instance"></param>
        /// <typeparam name="T"></typeparam>
        public static void WriteStruct<T>(BinaryWriter writer, T instance)
        {
            writer.Write(MarshalStruct(instance));
        }

        /// <summary>
        /// Marshal a structure to a byte array.
        /// </summary>
        /// <param name="instance"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static byte[] MarshalStruct<T>(T instance)
        {
            var size = Marshal.SizeOf(instance);
            var arr = new byte[size];
            var ptr = Marshal.AllocHGlobal(size);

            Marshal.StructureToPtr(instance, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);

            return arr;
        }

        /// <summary>
        /// Get the total length, in bytes, of a file.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static long GetFileLength(string file)
        {
            using (var reader = new BinaryReader(File.OpenRead(file)))
                return reader.BaseStream.Length;
        }

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

        public static string HexDump(byte[] bytes, int bytesPerLine = 16)
        {
            if (bytes == null) return "<null>";
            int bytesLength = bytes.Length;

            char[] HexChars = "0123456789ABCDEF".ToCharArray();

            int firstHexColumn =
                8 // 8 characters for the address
                + 3; // 3 spaces

            int firstCharColumn = firstHexColumn
                                  + bytesPerLine * 3 // - 2 digit for the hexadecimal value and 1 space
                                  + (bytesPerLine - 1) / 8 // - 1 extra space every 8 characters from the 9th
                                  + 2; // 2 spaces 

            int lineLength = firstCharColumn
                             + bytesPerLine // - characters to show the ascii value
                             + Environment.NewLine.Length; // Carriage return and line feed (should normally be 2)

            char[] line = (new String(' ', lineLength - Environment.NewLine.Length) + Environment.NewLine)
                .ToCharArray();
            int expectedLines = (bytesLength + bytesPerLine - 1) / bytesPerLine;
            StringBuilder result = new StringBuilder(expectedLines * lineLength);

            for (int i = 0; i < bytesLength; i += bytesPerLine)
            {
                line[0] = HexChars[(i >> 28) & 0xF];
                line[1] = HexChars[(i >> 24) & 0xF];
                line[2] = HexChars[(i >> 20) & 0xF];
                line[3] = HexChars[(i >> 16) & 0xF];
                line[4] = HexChars[(i >> 12) & 0xF];
                line[5] = HexChars[(i >> 8) & 0xF];
                line[6] = HexChars[(i >> 4) & 0xF];
                line[7] = HexChars[(i >> 0) & 0xF];

                int hexColumn = firstHexColumn;
                int charColumn = firstCharColumn;

                for (int j = 0; j < bytesPerLine; j++)
                {
                    if (j > 0 && (j & 7) == 0) hexColumn++;
                    if (i + j >= bytesLength)
                    {
                        line[hexColumn] = ' ';
                        line[hexColumn + 1] = ' ';
                        line[charColumn] = ' ';
                    }
                    else
                    {
                        byte b = bytes[i + j];
                        line[hexColumn] = HexChars[(b >> 4) & 0xF];
                        line[hexColumn + 1] = HexChars[b & 0xF];
                        line[charColumn] = (b < 32 ? '·' : (char)b);
                    }

                    hexColumn += 3;
                    charColumn++;
                }

                result.Append(line);
            }

            return result.ToString();
        }
    }
}
