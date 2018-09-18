using System;
using System.Text;

namespace Common
{
    public static class Hasher
    {
        public static uint BinHash(string k)
        {
            var hash = 0xFFFFFFFFu;
            var bytes = Encoding.GetEncoding(1252).GetBytes(k);

            for (var i = 0; i < k.Length; i++)
            {
                hash = bytes[i] + 33 * hash;
            }

            return hash;
        }

        public static ulong VltHash64(string k, ulong init = 0x11223344ABCDEF00)
        {
            int koffs = 0;
            int len = k.Length;
            ulong a = 0x9e3779b97f4a7c13;
            ulong b = a;
            ulong c = init;

            byte[] charArr = Encoding.ASCII.GetBytes(k);
            while (len >= 24)
            {
                a += BitConverter.ToUInt64(charArr, koffs);
                b += BitConverter.ToUInt64(charArr, koffs + 8);
                c += BitConverter.ToUInt64(charArr, koffs + 16);

                a -= b; a -= c; a ^= (c >> 43);
                b -= c; b -= a; b ^= (a << 9);
                c -= a; c -= b; c ^= (b >> 8);
                a -= b; a -= c; a ^= (c >> 38);
                b -= c; b -= a; b ^= (a << 23);
                c -= a; c -= b; c ^= (b >> 5);
                a -= b; a -= c; a ^= (c >> 35);
                b -= c; b -= a; b ^= (a << 49);
                c -= a; c -= b; c ^= (b >> 11);
                a -= b; a -= c; a ^= (c >> 12);
                b -= c; b -= a; b ^= (a << 18);
                c -= a; c -= b; c ^= (b >> 22);

                len -= 24;
                koffs += 24;
            }

            c += (ulong)k.Length;

            switch (len)
            {
                case 23:
                    c += ((ulong)k[22] << 56);
                    goto case 22;
                case 22:
                    c += ((ulong)k[21] << 48);
                    goto case 21;
                case 21:
                    c += ((ulong)k[20] << 40);
                    goto case 20;
                case 20:
                    c += ((ulong)k[19] << 32);
                    goto case 19;
                case 19:
                    c += ((ulong)k[18] << 24);
                    goto case 18;
                case 18:
                    c += ((ulong)k[17] << 16);
                    goto case 17;
                case 17:
                    c += ((ulong)k[16] << 8);
                    goto case 16;
                /* the first byte of c is reserved for the length */
                case 16:
                    b += ((ulong)k[15] << 56);
                    goto case 15;
                case 15:
                    b += ((ulong)k[14] << 48);
                    goto case 14;
                case 14:
                    b += ((ulong)k[13] << 40);
                    goto case 13;
                case 13:
                    b += ((ulong)k[12] << 32);
                    goto case 12;
                case 12:
                    b += ((ulong)k[11] << 24);
                    goto case 11;
                case 11:
                    b += ((ulong)k[10] << 16);
                    goto case 10;
                case 10:
                    b += ((ulong)k[9] << 8);
                    goto case 9;
                case 9:
                    b += ((ulong)k[8]);
                    goto case 8;
                case 8:
                    a += ((ulong)k[7] << 56);
                    goto case 7;
                case 7:
                    a += ((ulong)k[6] << 48);
                    goto case 6;
                case 6:
                    a += ((ulong)k[5] << 40);
                    goto case 5;
                case 5:
                    a += ((ulong)k[4] << 32);
                    goto case 4;
                case 4:
                    a += ((ulong)k[3] << 24);
                    goto case 3;
                case 3:
                    a += ((ulong)k[2] << 16);
                    goto case 2;
                case 2:
                    a += ((ulong)k[1] << 8);
                    goto case 1;
                case 1:
                    a += ((ulong)k[0]);
                    break;
            }

            a -= b; a -= c; a ^= (c >> 43);
            b -= c; b -= a; b ^= (a << 9);
            c -= a; c -= b; c ^= (b >> 8);
            a -= b; a -= c; a ^= (c >> 38);
            b -= c; b -= a; b ^= (a << 23);
            c -= a; c -= b; c ^= (b >> 5);
            a -= b; a -= c; a ^= (c >> 35);
            b -= c; b -= a; b ^= (a << 49);
            c -= a; c -= b; c ^= (b >> 11);
            a -= b; a -= c; a ^= (c >> 12);
            b -= c; b -= a; b ^= (a << 18);
            c -= a; c -= b; c ^= (b >> 22);

            return c;
        }

        public static uint VltHash(string k, uint init = 0xABCDEF00)
        {
            int koffs = 0;
            int len = k.Length;
            uint a = 0x9e3779b9;
            uint b = a;
            uint c = init;

            while (len >= 12)
            {
                a += (uint)k[0 + koffs] + ((uint)k[1 + koffs] << 8) + ((uint)k[2 + koffs] << 16) + ((uint)k[3 + koffs] << 24);
                b += (uint)k[4 + koffs] + ((uint)k[5 + koffs] << 8) + ((uint)k[6 + koffs] << 16) + ((uint)k[7 + koffs] << 24);
                c += (uint)k[8 + koffs] + ((uint)k[9 + koffs] << 8) + ((uint)k[10 + koffs] << 16) + ((uint)k[11 + koffs] << 24);

                a -= b; a -= c; a ^= (c >> 13);
                b -= c; b -= a; b ^= (a << 8);
                c -= a; c -= b; c ^= (b >> 13);
                a -= b; a -= c; a ^= (c >> 12);
                b -= c; b -= a; b ^= (a << 16);
                c -= a; c -= b; c ^= (b >> 5);
                a -= b; a -= c; a ^= (c >> 3);
                b -= c; b -= a; b ^= (a << 10);
                c -= a; c -= b; c ^= (b >> 15);

                koffs += 12;
                len -= 12;
            }

            c += (uint)k.Length;

            switch (len)
            {
                case 11:
                    c += (uint)k[10 + koffs] << 24;
                    goto case 10;
                case 10:
                    c += (uint)k[9 + koffs] << 16;
                    goto case 9;
                case 9:
                    c += (uint)k[8 + koffs] << 8;
                    goto case 8;
                case 8:
                    b += (uint)k[7 + koffs] << 24;
                    goto case 7;
                case 7:
                    b += (uint)k[6 + koffs] << 16;
                    goto case 6;
                case 6:
                    b += (uint)k[5 + koffs] << 8;
                    goto case 5;
                case 5:
                    b += (uint)k[4 + koffs];
                    goto case 4;
                case 4:
                    a += (uint)k[3 + koffs] << 24;
                    goto case 3;
                case 3:
                    a += (uint)k[2 + koffs] << 16;
                    goto case 2;
                case 2:
                    a += (uint)k[1 + koffs] << 8;
                    goto case 1;
                case 1:
                    a += (uint)k[0 + koffs];
                    break;
            }

            a -= b; a -= c; a ^= (c >> 13);
            b -= c; b -= a; b ^= (a << 8);
            c -= a; c -= b; c ^= (b >> 13);
            a -= b; a -= c; a ^= (c >> 12);
            b -= c; b -= a; b ^= (a << 16);
            c -= a; c -= b; c ^= (b >> 5);
            a -= b; a -= c; a ^= (c >> 3);
            b -= c; b -= a; b ^= (a << 10);
            c -= a; c -= b; c ^= (b >> 15);

            return c;
        }
    }
}
