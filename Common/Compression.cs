using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace Common
{
    public static class Compression
    {
        /// <summary>
        /// Cip = "Compress in place"
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct CipHeader
        {
            public uint CompressBlockMagic; // = 0x55441122
            public int USize; // =0x8000
            public int CSize; // Skip back to before CompressBlockMagic, then jump TotalBlockSize to get to the next block (or, subtract 24)
            public int UPos; // += OutSize
            public int CPos; // += TotalBlockSize
            public int Null; // = 0
        }

        /// <summary>
        /// Reads CIP (Compressed-In-Place) blocks from an input stream, and writes the decompressed data to an output stream.
        /// </summary>
        /// <param name="src">The input stream.</param>
        /// <param name="dst">The output stream.</param>
        /// <param name="srcSize">The maximum amount of bytes that should be read from the input stream.</param>
        /// <param name="decompressedSize">Receives the amount of decompressed bytes</param>
        /// <exception cref="ManagedCompressionException">if compressed data cannot be fully read from the input stream</exception>
        public static void DecompressCip(Stream src, Stream dst, int srcSize, out long decompressedSize)
        {
            decompressedSize = 0;

            var sbr = new BinaryReader(src);
            var srcEnd = src.Position + srcSize;

            while (src.Position < srcEnd)
            {
                var cipHeader = BinaryUtil.ReadStruct<CipHeader>(sbr);
                var compressedBytes = new byte[cipHeader.CSize - 24];

                if (sbr.Read(compressedBytes, 0, compressedBytes.Length) != compressedBytes.Length)
                {
                    throw new ManagedCompressionException($"Could not read {compressedBytes.Length} compressed bytes from source stream");
                }

                dst.Seek(cipHeader.UPos, SeekOrigin.Begin);

                var decompressedBytes = new byte[cipHeader.USize];
                Decompress(compressedBytes, decompressedBytes);
                dst.Write(decompressedBytes, 0, decompressedBytes.Length);

                decompressedSize += decompressedBytes.Length;
            }

            dst.Seek(0, SeekOrigin.Begin);
        }

        [DllImport("complib", EntryPoint = "LZDecompress", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Decompress(
            [In] byte[] inData,
            [Out] byte[] outData
        );
    }

    [Serializable]
    public class ManagedCompressionException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public ManagedCompressionException()
        {
        }

        public ManagedCompressionException(string message) : base(message)
        {
        }

        public ManagedCompressionException(string message, Exception inner) : base(message, inner)
        {
        }

        protected ManagedCompressionException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}
