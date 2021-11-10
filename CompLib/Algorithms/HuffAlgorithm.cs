using System;

namespace CompLib.Algorithms
{
    public class HuffAlgorithm : ICompressionAlgorithm
    {
        private ref struct BitStream
        {
            private readonly ReadOnlySpan<byte> _buffer;
            private int _offset;
            public int BitsLeft;
            public uint UnshiftedBits;
            public uint Bits;

            public BitStream(ReadOnlySpan<byte> buffer)
            {
                _buffer = buffer;
                _offset = 0;
                UnshiftedBits = 0;
                Bits = 0;
                BitsLeft = -16;
            }

            private byte? ReadNextByte()
            {
                if (_offset < _buffer.Length)
                {
                    return _buffer[_offset++];
                }

                return null;
            }

            private void ReadSegmentPart()
            {
                UnshiftedBits = (ReadNextByte() ?? 0) | (UnshiftedBits << 8);
            }

            public void ReadSegment()
            {
                ReadSegmentPart();
                ReadSegmentPart();
            }

            public uint ReadBits(int n)
            {
                if (n is < 0 or > 32)
                {
                    throw new ArgumentException($"Tried to read {n} bits, outside the valid range [0, 32]");
                }

                var val = 0u;

                if (n > 0)
                {
                    val = Bits >> (32 - n);
                    Bits <<= n;
                    BitsLeft -= n;
                }

                if (BitsLeft >= 0) return val;

                ReadSegment();
                Bits = UnshiftedBits << -BitsLeft;
                BitsLeft += 16;

                return val;
            }

            public uint ReadNum()
            {
                if (unchecked((int)Bits) < 0)
                {
                    return ReadBits(3) - 4;
                }

                int n;
                uint v;

                if (Bits >> 16 != 0)
                {
                    n = 2;
                    do
                    {
                        Bits <<= 1;
                        n++;
                    } while (unchecked((int)Bits) >= 0);

                    Bits <<= 1;
                    BitsLeft -= n - 1;
                    v = ReadBits(0);
                }
                else
                {
                    n = 2;
                    do
                    {
                        n++;
                        v = ReadBits(1);
                    } while (v == 0);
                }

                if (n > 16)
                {
                    v = (uint)((ReadBits(16) | (ReadBits(n - 16) << 16)) + (1 << n) - 4);
                }
                else
                {
                    v = ReadBits(n);
                    v = (uint)(v + (1 << n) - 4);
                }

                return v;
            }

            public void RefreshBits()
            {
                ReadSegment();
                Bits = UnshiftedBits << (16 - BitsLeft);
            }
        }

        public int Decompress(ReadOnlySpan<byte> input, Span<byte> output)
        {
            var bs = new BitStream(input);

            bs.ReadBits(0);

            var huffType = bs.ReadBits(16);

            if ((huffType & 0x100) == 0x100)
            {
                throw new CompressionException($"Unsupported HUFF flags: 0x{huffType:X}");
            }

            var decompressedLen = (huffType & 0x8000) switch
            {
                0x8000 => (bs.ReadBits(16) << 16) | bs.ReadBits(16),
                _ => (bs.ReadBits(8) << 16) | bs.ReadBits(16)
            };

            if (decompressedLen != output.Length)
            {
                throw new CompressionException(
                    $"Expected output buffer to be {decompressedLen} bytes long, but it's {output.Length} bytes long");
            }

            var decompressedOffset = 0;
            var numChars = 0u;
            var numBits = 1;
            var clue = (byte)bs.ReadBits(8);
            var baseCmp = 0u;

            Span<int> bitNumTable = stackalloc int[16];
            Span<uint> deltaTable = stackalloc uint[16];
            Span<uint> cmpTable = stackalloc uint[16];
            Span<byte> codeTable = stackalloc byte[256];
            Span<byte> quickCodeTable = stackalloc byte[256];
            Span<byte> quickLenTable = stackalloc byte[256];
            Span<sbyte> leap = stackalloc sbyte[256];

            quickLenTable.Fill(0x40);

            for (;;)
            {
                baseCmp <<= 1;
                deltaTable[numBits] = baseCmp - numChars;
                var bitNum = bs.ReadNum();
                bitNumTable[numBits] = (int)bitNum;
                numChars += bitNum;
                baseCmp += bitNum;

                var cmp = bitNum switch
                {
                    0 => 0u,
                    _ => (baseCmp << (16 - numBits)) & 0xFFFF
                };

                cmpTable[numBits++] = cmp;

                if (bitNum != 0 && cmp == 0)
                {
                    break;
                }
            }

            cmpTable[numBits - 1] = 0xffffffff;

            var mostBits = numBits - 1;
            byte nextChar = 0xFF;

            for (var i = 0; i < numChars; i++)
            {
                var leapDelta = unchecked((int)bs.ReadNum()) + 1;

                do
                {
                    nextChar++;

                    if (leap[nextChar] == 0)
                    {
                        leapDelta--;
                    }
                } while (leapDelta != 0);

                leap[nextChar] = 1;
                codeTable[i] = nextChar;
            }

            byte bits = 1;
            var codeTableIndex = 0;
            var quickTablesIndex = 0;
            var clueLen = 0;

            while (bits <= mostBits && bits < 9)
            {
                var bitNum = bitNumTable[bits];
                var numBitEntries = 1 << (8 - bits);

                while (bitNum-- > 0)
                {
                    var nextCode = codeTable[codeTableIndex++];
                    var nextLen = bits;

                    if (nextCode == clue)
                    {
                        clueLen = bits;
                        nextLen = 96;
                    }

                    var quickRange = quickTablesIndex..(quickTablesIndex + numBitEntries);
                    quickCodeTable[quickRange].Fill(nextCode);
                    quickLenTable[quickRange].Fill(nextLen);

                    quickTablesIndex += numBitEntries;
                }

                bits++;
            }

            // Main decoder
            for (;;)
            {
                // 8-bit fetch + decode
                for (;;)
                {
                    numBits = quickLenTable[(int)(bs.Bits >> 24)];
                    bs.BitsLeft -= numBits;
                    
                    while (bs.BitsLeft >= 0)
                    {
                        output[decompressedOffset++] = quickCodeTable[(int)(bs.Bits >> 24)];
                        bs.Bits <<= numBits;
                        numBits = quickLenTable[(int)(bs.Bits >> 24)];
                        bs.BitsLeft -= numBits;
                    }

                    bs.BitsLeft += 16;
                    if (bs.BitsLeft < 0)
                        break;
                    output[decompressedOffset++] = quickCodeTable[(int)(bs.Bits >> 24)];
                    bs.RefreshBits();
                }

                bs.BitsLeft = bs.BitsLeft - 16 + numBits;

                uint cmp;

                if (numBits != 96)
                {
                    cmp = bs.Bits >> 16;
                    numBits = 8;
                    do
                    {
                        numBits++;
                    } while (cmp >= cmpTable[numBits]);
                }
                else
                {
                    numBits = clueLen;
                }

                cmp = bs.Bits >> (32 - numBits);
                bs.Bits <<= numBits;
                bs.BitsLeft -= numBits;

                var code = codeTable[(int)(cmp - deltaTable[numBits])];

                if (code != clue && bs.BitsLeft >= 0)
                {
                    output[decompressedOffset++] = code;
                    continue; // go back to 8-bit fetch + decode
                }

                if (bs.BitsLeft < 0)
                {
                    bs.ReadSegment();
                    bs.Bits = bs.UnshiftedBits << -bs.BitsLeft;
                    bs.BitsLeft += 16;
                }

                if (code != clue)
                {
                    output[decompressedOffset++] = code;
                    continue; // go back to 8-bit fetch + decode
                }

                // handle clue
                var runLen = (int)bs.ReadNum();
                if (runLen > 0)
                {
                    var runCode = output[decompressedOffset - 1];
                    output[decompressedOffset..(decompressedOffset + runLen)].Fill(runCode);

                    decompressedOffset += runLen;
                    continue; // go back to 8-bit fetch + decode
                }

                if (bs.ReadBits(1) != 0)
                {
                    break;
                }

                output[decompressedOffset++] = (byte)bs.ReadBits(8);
            }

            switch (huffType & ~0x8000)
            {
                case 0x32fb:
                {
                    var code = 0u;
                    for (int i = 0; i < decompressedLen; i++)
                    {
                        code += output[i];
                        output[i] = (byte)code;
                    }

                    break;
                }
                case 0x34fb:
                {
                    var code = 0u;
                    var nc = 0u;
                    for (int i = 0; i < decompressedLen; i++)
                    {
                        code += output[i];
                        nc += code;
                        output[i] = (byte)nc;
                    }

                    break;
                }
            }

            return decompressedOffset;
        }

        public int Compress(ReadOnlySpan<byte> input, Span<byte> output)
        {
            throw new NotImplementedException();
        }
    }
}