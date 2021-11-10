using System;

namespace CompLib.Algorithms
{
    public class RefPackAlgorithm : ICompressionAlgorithm
    {
        public int Decompress(ReadOnlySpan<byte> input, Span<byte> output)
        {
            var inputOffset = 0;
            var outputOffset = 0;
            var refType = (input[inputOffset++] << 8) | input[inputOffset++];

            if ((refType & 0x100) == 0x100)
            {
                throw new CompressionException($"Unsupported RefPack flags: 0x{refType:X}");
            }

            var decompressedLen = (refType & 0x8000) switch
            {
                0x8000 => (input[inputOffset++] << 24) | (input[inputOffset++] << 16) | (input[inputOffset++] << 8) |
                          input[inputOffset++],
                _ => (input[inputOffset++] << 16) | (input[inputOffset++] << 8) | input[inputOffset++]
            };

            if (decompressedLen != output.Length)
            {
                throw new CompressionException(
                    $"Expected output buffer to be {decompressedLen} bytes long, but it's {output.Length} bytes long");
            }

            var consumeTokens = true;

            while (consumeTokens)
            {
                var prefixByte = input[inputOffset++];
                var literalCopyLen = 0;
                var refCopyLen = 0;
                var refCopyOffset = 0;

                if ((prefixByte & 0x80) == 0)
                {
                    // Short form
                    literalCopyLen = prefixByte & 3;
                    refCopyLen = ((prefixByte & 0x1c) >> 2) + 3;
                    refCopyOffset = ((prefixByte & 0x60) << 3) | input[inputOffset++];
                }
                else if ((prefixByte & 0x40) == 0)
                {
                    // Long form
                    byte secondByte = input[inputOffset++], thirdByte = input[inputOffset++];
                    literalCopyLen = secondByte >> 6;
                    refCopyLen = (prefixByte & 0x3f) + 4;
                    refCopyOffset = ((secondByte & 0x3f) << 8) | thirdByte;
                }
                else if ((prefixByte & 0x20) == 0)
                {
                    // Very long form
                    byte secondByte = input[inputOffset++],
                        thirdByte = input[inputOffset++],
                        fourthByte = input[inputOffset++];
                    literalCopyLen = prefixByte & 3;
                    refCopyLen = (((prefixByte & 0x0c) >> 2 << 8) | fourthByte) + 5;
                    refCopyOffset = ((prefixByte & 0x10) >> 4 << 16) | (secondByte << 8) | thirdByte;
                }
                else
                {
                    literalCopyLen = ((prefixByte & 0x1f) << 2) + 4;

                    if (literalCopyLen > 112)
                    {
                        literalCopyLen = prefixByte & 3;
                        consumeTokens = false;
                    }
                }

                if (literalCopyLen > 0)
                {
                    input[inputOffset..(inputOffset + literalCopyLen)]
                        .CopyTo(output[outputOffset..(outputOffset + literalCopyLen)]);
                    inputOffset += literalCopyLen;
                    outputOffset += literalCopyLen;
                }

                if (refCopyLen > 0)
                {
                    for (var i = 0; i < refCopyLen; i++)
                    {
                        output[outputOffset + i] = output[outputOffset - refCopyOffset - 1 + i];
                    }

                    outputOffset += refCopyLen;
                }
            }

            return outputOffset;
        }

        public int Compress(ReadOnlySpan<byte> input, Span<byte> output)
        {
            throw new NotImplementedException();
        }
    }
}