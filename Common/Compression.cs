using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Common
{
    public static class JDLZ
    {
        private const int HeaderSize = 16;

        public static byte[] Decompress(byte[] input)
        {
            // Sanity checking...
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            if (input.Length < HeaderSize || input[0] != 'J' || input[1] != 'D' || input[2] != 'L' ||
                input[3] != 'Z' || input[4] != 0x02)
            {
                throw new InvalidDataException("Input header is not JDLZ!");
            }

            int flags1 = 1, flags2 = 1;
            int inPos = HeaderSize, outPos = 0;

            // TODO: Can we always trust the header's stated length?
            var output = new byte[BitConverter.ToInt32(input, 8)];

            while (inPos < input.Length && outPos < output.Length)
            {
                if (flags1 == 1)
                {
                    flags1 = input[inPos++] | 0x100;
                }

                if (flags2 == 1)
                {
                    flags2 = input[inPos++] | 0x100;
                }

                if ((flags1 & 1) == 1)
                {
                    int length;
                    int t;
                    if ((flags2 & 1) == 1) // 3 to 4098(?) iterations, backtracks 1 to 16(?) bytes
                    {
                        // length max is 4098(?) (0x1002), assuming input[inPos] and input[inPos + 1] are both 0xFF
                        length = (input[inPos + 1] | ((input[inPos] & 0xF0) << 4)) + 3;
                        // t max is 16(?) (0x10), assuming input[inPos] is 0xFF
                        t = (input[inPos] & 0x0F) + 1;
                    }
                    else // 3(?) to 34(?) iterations, backtracks 17(?) to 2064(?) bytes
                    {
                        // t max is 2064(?) (0x810), assuming input[inPos] and input[inPos + 1] are both 0xFF
                        t = (input[inPos + 1] | ((input[inPos] & 0xE0) << 3)) + 17;
                        // length max is 34(?) (0x22), assuming input[inPos] is 0xFF
                        length = (input[inPos] & 0x1F) + 3;
                    }

                    inPos += 2;

                    for (var i = 0; i < length; ++i)
                    {
                        output[outPos + i] = output[outPos + i - t];
                    }

                    outPos += length;
                    flags2 >>= 1;
                }
                else
                {
                    if (outPos < output.Length)
                    {
                        output[outPos++] = input[inPos++];
                    }
                }

                flags1 >>= 1;
            }

            return output;
        }

        /// <summary>
        /// This lovely JDLZ compressor was written by the user "zombie28" of the encode.ru forums.
        /// Its compression ratios are within a few percent of the NFS games' JDLZ compressor. Awesome!!
        /// </summary>
        /// <remarks>heyitsleo - This is useless now. Keeping it for reference.</remarks>
        /// <param name="input">bytes to compress with JDLZ</param>
        /// <param name="hashSize">speed/ratio tunable; use powers of 2. results vary per file.</param>
        /// <param name="maxSearchDepth">speed/ratio tunable. results vary per file.</param>
        /// <returns>JDLZ-compressed bytes, w/ 16 byte header</returns>
        public static byte[] Compress(byte[] input, int hashSize = 0x2000, int maxSearchDepth = 16)
        {
            // Sanity checking...
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            const int minMatchLength = 3;

            var inputBytes = input.Length;
            var output = new byte[inputBytes + ((inputBytes + 7) / 8) + HeaderSize + 1];
            var hashPos = new int[hashSize];
            var hashChain = new int[inputBytes];

            var outPos = 0;
            var inPos = 0;
            byte flags1Bit = 1;
            byte flags2Bit = 1;
            byte flags1 = 0;
            byte flags2 = 0;

            output[outPos++] = 0x4A; // 'J'
            output[outPos++] = 0x44; // 'D'
            output[outPos++] = 0x4C; // 'L'
            output[outPos++] = 0x5A; // 'Z'
            output[outPos++] = 0x02;
            output[outPos++] = 0x10;
            output[outPos++] = 0x00;
            output[outPos++] = 0x00;
            output[outPos++] = (byte)inputBytes;
            output[outPos++] = (byte)(inputBytes >> 8);
            output[outPos++] = (byte)(inputBytes >> 16);
            output[outPos++] = (byte)(inputBytes >> 24);
            outPos += 4;

            var flags1Pos = outPos++;
            var flags2Pos = outPos++;

            flags1Bit <<= 1;
            output[outPos++] = input[inPos++];
            inputBytes--;

            while (inputBytes > 0)
            {
                var bestMatchLength = minMatchLength - 1;
                var bestMatchDist = 0;

                if (inputBytes >= minMatchLength)
                {
                    var hash = (-0x1A1 * (input[inPos] ^ ((input[inPos + 1] ^ (input[inPos + 2] << 4)) << 4))) &
                               (hashSize - 1);
                    var matchPos = hashPos[hash];
                    hashPos[hash] = inPos;
                    hashChain[inPos] = matchPos;
                    var prevMatchPos = inPos;

                    for (var i = 0; i < maxSearchDepth; i++)
                    {
                        var matchDist = inPos - matchPos;

                        if (matchDist > 2064 || matchPos >= prevMatchPos)
                        {
                            break;
                        }

                        var matchLengthLimit = matchDist <= 16 ? 4098 : 34;
                        var maxMatchLength = inputBytes;

                        if (maxMatchLength > matchLengthLimit)
                        {
                            maxMatchLength = matchLengthLimit;
                        }

                        if (bestMatchLength >= maxMatchLength)
                        {
                            break;
                        }

                        var matchLength = 0;
                        while ((matchLength < maxMatchLength) &&
                               (input[inPos + matchLength] == input[matchPos + matchLength]))
                        {
                            matchLength++;
                        }

                        if (matchLength > bestMatchLength)
                        {
                            bestMatchLength = matchLength;
                            bestMatchDist = matchDist;
                        }

                        prevMatchPos = matchPos;
                        matchPos = hashChain[matchPos];
                    }
                }

                if (bestMatchLength >= minMatchLength)
                {
                    flags1 |= flags1Bit;
                    inPos += bestMatchLength;
                    inputBytes -= bestMatchLength;
                    bestMatchLength -= minMatchLength;

                    if (bestMatchDist < 17)
                    {
                        flags2 |= flags2Bit;
                        output[outPos++] = (byte)((bestMatchDist - 1) | ((bestMatchLength >> 4) & 0xF0));
                        output[outPos++] = (byte)bestMatchLength;
                    }
                    else
                    {
                        bestMatchDist -= 17;
                        output[outPos++] = (byte)(bestMatchLength | ((bestMatchDist >> 3) & 0xE0));
                        output[outPos++] = (byte)bestMatchDist;
                    }

                    flags2Bit <<= 1;
                }
                else
                {
                    output[outPos++] = input[inPos++];
                    inputBytes--;
                }

                flags1Bit <<= 1;

                if (flags1Bit == 0)
                {
                    output[flags1Pos] = flags1;
                    flags1 = 0;
                    flags1Pos = outPos++;
                    flags1Bit = 1;
                }

                if (flags2Bit != 0) continue;
                output[flags2Pos] = flags2;
                flags2 = 0;
                flags2Pos = outPos++;
                flags2Bit = 1;
            }

            if (flags2Bit > 1)
            {
                output[flags2Pos] = flags2;
            }
            else if (flags2Pos == outPos - 1)
            {
                outPos = flags2Pos;
            }

            if (flags1Bit > 1)
            {
                output[flags1Pos] = flags1;
            }
            else if (flags1Pos == outPos - 1)
            {
                outPos = flags1Pos;
            }

            output[12] = (byte)outPos;
            output[13] = (byte)(outPos >> 8);
            output[14] = (byte)(outPos >> 16);
            output[15] = (byte)(outPos >> 24);

            Array.Resize(ref output, outPos);
            return output;
        }
    }

    public static class Compression
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct CompressBlockHead
        {
            public uint CompressBlockMagic; // = 0x55441122
            public int USize; // =0x8000
            public int CSize; // Skip back to before CompressBlockMagic, then jump TotalBlockSize to get to the next block (or, subtract 24)
            public int UPos; // += OutSize
            public int CPos; // += TotalBlockSize
            public uint Unknown4;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct SimpleCompressionHeader
        {
            public uint CompressionType;
            public uint Unknown;
            public uint OutLength;
            public uint CompressedLength;

            //    if (flag == 0x5a4c444a) // JDLZ
            //{
            //    br.BaseStream.Position -= 4;

            //    br.BaseStream.Position += 0x8;

            //    outSize = br.ReadUInt32();
            //} else if (flag == 0x46465548) // HUFF
            //{
            //    br.BaseStream.Position += 4;

            //    outSize = br.ReadUInt32();

            //    br.BaseStream.Position += 4;
            //}
        }

        [DllImport("complib", EntryPoint = "LZDecompress", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Decompress(
            [In] byte[] inData,
            [Out] byte[] outData
        );
    }
}
