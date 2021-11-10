using System;

namespace CompLib.Algorithms
{
    public class RawAlgorithm : ICompressionAlgorithm
    {
        public int Decompress(ReadOnlySpan<byte> input, Span<byte> output)
        {
            input.CopyTo(output);
            return output.Length;
        }

        public int Compress(ReadOnlySpan<byte> input, Span<byte> output)
        {
            throw new NotImplementedException();
        }
    }
}