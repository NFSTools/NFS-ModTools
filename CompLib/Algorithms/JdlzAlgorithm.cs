using System;

namespace CompLib.Algorithms
{
    public class JdlzAlgorithm : ICompressionAlgorithm
    {
        private const int NearRunBits = 12;
        private const int NearOffBits = 16 - NearRunBits;
        private const int NearRunMax = (1 << NearRunBits) + 2;
        private const int NearOffMax = 1 << NearOffBits;
        private const int NearRunMask = (1 << NearOffBits) - 1;

        private const int FarRunBits = 5;
        private const int FarRunMax = (1 << FarRunBits) + 2;
        private const int FarRunMask = (1 << FarRunBits) - 1;

        public int Decompress(ReadOnlySpan<byte> input, Span<byte> output)
        {
            var inputOffset = 0;
            var outputOffset = 0;
            var control = 0x100u | input[inputOffset++];
            var runType = 0x100u | input[inputOffset++];

            for (; inputOffset < input.Length && outputOffset < output.Length;)
            {
                if ((control & 1) == 0)
                {
                    // Literal block copy
                    do
                    {
                        output[outputOffset++] = input[inputOffset++];
                        control >>= 1;
                    } while ((control & 1) == 0 && outputOffset < output.Length);

                    if (control == 1)
                    {
                        control = 0x100u | input[inputOffset++];
                    }

                    continue;
                }

                uint runCode = input[inputOffset++];
                int runLength, runOffset;

                if ((runType & 1) != 0)
                {
                    runOffset = (int)((runCode & NearRunMask) + 1);
                    runLength = (int)((((runCode >> NearOffBits) << 8) | input[inputOffset++]) + 3);
                }
                else
                {
                    runOffset = (int)((((runCode >> FarRunBits) << 8) | input[inputOffset++]) + NearOffMax + 1);
                    runLength = (int)((runCode & FarRunMask) + 3);
                }

                // Back-reference run copy
                for (var i = 0; i < runLength; i++)
                {
                    output[outputOffset + i] = output[outputOffset - runOffset + i];
                }

                outputOffset += runLength;
                
                control >>= 1;
                if (control == 1)
                {
                    control = 0x100u | input[inputOffset++];
                }
                
                runType >>= 1;
                if (runType == 1)
                {
                    runType = 0x100u | input[inputOffset++];
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