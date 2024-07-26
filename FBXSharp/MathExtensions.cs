using System;
using System.Text;

namespace FBXSharp
{
    public static class MathExtensions
    {
        private static readonly double[] ms_powersDoubleNeg10 =
        {
            0.0,
            0.1d,
            0.01d,
            0.001d,
            0.0001d,
            0.00001d,
            0.000001d,
            0.0000001d,
            0.00000001d,
            0.000000001d,
            0.0000000001d,
            0.00000000001d,
            0.000000000001d,
            0.0000000000001d,
            0.00000000000001d,
            0.000000000000001d,
            0.0000000000000001d,
            0.00000000000000001d,
            0.000000000000000001d,
            0.0000000000000000001d,
            0.00000000000000000001d,
            0.000000000000000000001d,
            0.0000000000000000000001d,
            0.00000000000000000000001d,
            0.000000000000000000000001d,
            0.0000000000000000000000001d,
            0.00000000000000000000000001d,
            0.000000000000000000000000001d,
            0.0000000000000000000000000001d,
            0.00000000000000000000000000001d,
            0.000000000000000000000000000001d,
            0.0000000000000000000000000000001d,
            0.00000000000000000000000000000001d,
            0.000000000000000000000000000000001d,
            0.0000000000000000000000000000000001d,
            0.00000000000000000000000000000000001d,
            0.000000000000000000000000000000000001d,
            0.0000000000000000000000000000000000001d,
            0.00000000000000000000000000000000000001d,
            0.000000000000000000000000000000000000001d,
            0.0000000000000000000000000000000000000001d,
            0.00000000000000000000000000000000000000001d
        };

        private static readonly double[] ms_powersDoublePos10 =
        {
            0.0d,
            10.0d,
            100.0d,
            1000.0d,
            10000.0d,
            100000.0d,
            1000000.0d,
            10000000.0d,
            100000000.0d,
            1000000000.0d,
            10000000000.0d,
            100000000000.0d,
            1000000000000.0d,
            10000000000000.0d,
            100000000000000.0d,
            1000000000000000.0d,
            10000000000000000.0d,
            100000000000000000.0d,
            1000000000000000000.0d,
            10000000000000000000.0d,
            100000000000000000000.0d,
            1000000000000000000000.0d,
            10000000000000000000000.0d,
            100000000000000000000000.0d,
            1000000000000000000000000.0d,
            10000000000000000000000000.0d,
            100000000000000000000000000.0d,
            1000000000000000000000000000.0d,
            10000000000000000000000000000.0d,
            100000000000000000000000000000.0d,
            1000000000000000000000000000000.0d,
            10000000000000000000000000000000.0d,
            100000000000000000000000000000000.0d,
            1000000000000000000000000000000000.0d,
            10000000000000000000000000000000000.0d,
            100000000000000000000000000000000000.0d,
            1000000000000000000000000000000000000.0d,
            10000000000000000000000000000000000000.0d,
            100000000000000000000000000000000000000.0d,
            1000000000000000000000000000000000000000.0d,
            10000000000000000000000000000000000000000.0d
        };

        public static double FastDoublePow10(int power)
        {
            if (power < 0)
            {
                if (power < -40)
                    return Math.Pow(10.0d, power);
                return ms_powersDoubleNeg10[-power];
            }

            if (power > 0)
            {
                if (power > 40)
                    return Math.Pow(10.0d, power);
                return ms_powersDoublePos10[power];
            }

            return 1.0d; // power == 0
        }

        public static float FastSinglePow10(int power)
        {
            if (power < 0)
            {
                if (power < -35)
                    return (float)Math.Pow(10.0f, power);
                return (float)ms_powersDoubleNeg10[-power];
            }

            if (power > 0)
            {
                if (power > 35)
                    return (float)Math.Pow(10.0f, power);
                return (float)ms_powersDoublePos10[power];
            }

            return 1.0f;
        }

        public static double FBXTimeToSeconds(long value)
        {
            return (double)value / 46186158000L;
        }

        public static long SecondsToFBXTime(double value)
        {
            return (long)(value * 46186158000L);
        }

        public static int SumTo(this int[][] array, int index)
        {
            var count = 0;

            for (var i = 0; i < index; ++i) count += array[i].Length;

            return count;
        }

        public static string Join(this string[] array, string separator)
        {
            var builder = new StringBuilder();

            for (var i = 0; i < array.Length; ++i)
            {
                builder.Append(array[i]);
                builder.Append(separator);
            }

            return builder.ToString();
        }

        public static string Substring(this string str, string splitter)
        {
            var index = str.IndexOf(splitter);

            return index < 0 ? str : str.Substring(0, index);
        }
    }
}