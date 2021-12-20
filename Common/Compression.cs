using System;
using System.IO;
using CompLib;

namespace Common
{
    /// <summary>
    /// Wrapper around CompLib API 
    /// </summary>
    public static class Compression
    {
        public static Span<byte> Decompress(ReadOnlySpan<byte> input)
        {
            return BlobDecompressor.Decompress(input);
        }

        public static long DecompressCip(Stream src, Stream dst, long srcSize)
        {
            CipDecompressor.Decompress(src, dst, srcSize, out var decompressedSize);

            return decompressedSize;
        }
    }
}