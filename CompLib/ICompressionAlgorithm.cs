using System;

namespace CompLib
{
    public interface ICompressionAlgorithm
    {
        /// <summary>
        /// Decompresses data from an input buffer into an output buffer.
        /// </summary>
        /// <param name="input">The data to decompress.</param>
        /// <param name="output">The destination of the decompressed data.</param>
        /// <returns>The number of bytes that were decompressed</returns>
        /// <remarks>The backing buffer for <paramref name="output"/> must be sufficiently large to store the decompressed data.</remarks>
        int Decompress(ReadOnlySpan<byte> input, Span<byte> output);

        /// <summary>
        /// Compresses data from an input buffer into an output buffer.
        /// </summary>
        /// <param name="input">The data to compress.</param>
        /// <param name="output">The destination of the compressed data.</param>
        /// <returns>The length of the compressed data</returns>
        /// <remarks>The backing buffer for <paramref name="output"/> must be sufficiently large to store the compressed data.</remarks>
        int Compress(ReadOnlySpan<byte> input, Span<byte> output);
    }
}