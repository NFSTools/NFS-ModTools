using System;
using System.Runtime.InteropServices;
using CompLib.Algorithms;

namespace CompLib
{
    /// <summary>
    /// Helper class for automatically detecting and decoding supported compression formats.
    /// </summary>
    public static class Decompressor
    {
        /// <summary>
        /// Decompresses the given data.
        /// </summary>
        /// <param name="input">The data to decompress, including the compression header.</param>
        /// <returns>The decompressed data</returns>
        /// <exception cref="CompressionException">if a validation or other anticipated error occurred</exception>
        public static Span<byte> Decompress(ReadOnlySpan<byte> input)
        {
            if (input.Length < 16)
            {
                throw new CompressionException(
                    $"Expected at least 16 bytes to be passed to decompressor, but only got {input.Length}.");
            }

            var header = MemoryMarshal.Read<CompressedDataHeader>(input);
            ICompressionAlgorithm algorithm = header.ID switch
            {
                0x5A4C444A => new JdlzAlgorithm(),
                0x46465548 => new HuffAlgorithm(),
                0x4B504652 => new RefPackAlgorithm(),
                0x57574152 => new RawAlgorithm(),
                _ => throw new CompressionException($"Unrecognized compression algorithm: 0x{header.ID:X8}")
            };

            Span<byte> output = new byte[header.UncompressedSize];
            var decompressedLength = algorithm.Decompress(input[header.HeaderSize..], output);
            
            if (decompressedLength != output.Length)
            {
                throw new CompressionException($"Expected to end up with {output.Length} decompressed bytes, but only got {decompressedLength} bytes");
            }

            return output;
        }
    }
}