using System.Runtime.InteropServices;

namespace FBXSharp.ValueTypes
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Half
    {
        [StructLayout(LayoutKind.Explicit, Pack = 1)]
        private ref struct Union
        {
            [FieldOffset(0)] public uint UInt;

            [FieldOffset(0)] public float Float;

            public Union(float value)
            {
                UInt = 0;
                Float = value;
            }

            public Union(uint value)
            {
                Float = 0.0f;
                UInt = value;
            }
        }

        public const int SizeOf = 0x02;

        public readonly ushort Value;

        public Half(short value)
        {
            Value = (ushort)value;
        }

        public Half(ushort value)
        {
            Value = value;
        }

        public Half(float value)
        {
            var union = new Union(value);

            var sign = union.UInt >> 31;
            var expt = union.UInt & 0x7F800000;
            var mant = union.UInt & 0x007FFFFF;

            if (expt < 0x47800000u)
            {
                if (expt > 0x38000000u)
                    Value = (ushort)((mant >> 13) | ((expt - 0x38000000u) >> 13) | (sign << 15));
                else
                    Value = (ushort)((mant >> (int)(((0x38000000u - expt) >> 23) + 14u)) | (sign << 15));
            }
            else
            {
                var max = mant != 0u && expt == 0x7F800000u ? 0x007FFFFFu : 0u;

                Value = (ushort)((max >> 13) | (sign << 15) | 0x7C00u);
            }
        }

        public float ToSingle()
        {
            var expt = Value & 0x7C00u;
            var mant = Value & 0x3FFu;

            if (expt == 0x7C00u)
            {
                expt = 0x7F800000u;

                if ((Value & 0x3FFu) != 0) mant = 0x7FFFFFu;
            }
            else if ((Value & 0x7C00u) != 0)
            {
                mant <<= 13;
                expt = (expt << 13) + 0x38000000u;
            }
            else if ((Value & 0x3FF) != 0)
            {
                var mu = mant << 1;
                expt = 0x38000000u;

                while ((mu & 0x400u) == 0)
                {
                    mu <<= 1;
                    expt -= 0x800000u;
                }

                mant = (mu & 0x3FFu) << 13;
            }

            return new Union(mant | expt | (((uint)Value >> 15) << 31)).Float;
        }
    }
}