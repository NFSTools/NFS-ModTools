using System;
using System.Numerics;

namespace Common
{
    public static class MathUtil
    {
        public static Quaternion LookRotation(float m00, float m02, float m01, float m20, float m22, float m21, float m10, float m12, float m11)
        {
            float num8 = m00 + m11 + m22;
            float x, y, z, w;

            if (num8 > 0f)
            {
                var num = (float) Math.Sqrt(num8 + 1f);
                w = num * 0.5f;
                num = 0.5f / num;
                x = (m12 - m21) * num;
                y = (m20 - m02) * num;
                z = (m01 - m10) * num;
                return new Quaternion(x, z, y, w);
            }
            if (m00 >= m11 && m00 >= m22)
            {
                var num7 = (float) Math.Sqrt(1f + m00 - m11 - m22);
                var num4 = 0.5f / num7;
                x = 0.5f * num7;
                y = (m01 + m10) * num4;
                z = (m02 + m20) * num4;
                w = (m12 - m21) * num4;
                return new Quaternion(x, z, y, w);
            }
            if (m11 > m22)
            {
                var num6 = (float) Math.Sqrt(1f + m11 - m00 - m22);
                var num3 = 0.5f / num6;
                x = (m10 + m01) * num3;
                y = 0.5f * num6;
                z = (m21 + m12) * num3;
                w = (m20 - m02) * num3;
                return new Quaternion(x, z, y, w);
            }

            var num5 = (float) Math.Sqrt(1f + m22 - m00 - m11);
            var num2 = 0.5f / num5;
            x = (m20 + m02) * num2;
            y = (m21 + m12) * num2;
            z = 0.5f * num5;
            w = (m01 - m10) * num2;
            return new Quaternion(x, z, y, w);
        }
    }
}