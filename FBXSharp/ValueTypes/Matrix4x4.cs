using System;
using System.Runtime.InteropServices;

namespace FBXSharp.ValueTypes
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Matrix4x4 : IEquatable<Matrix4x4>
    {
        public const int SizeOf = 0x80;

        public static readonly Matrix4x4 Identity = new Matrix4x4
        (
            1.0, 0.0, 0.0, 0.0,
            0.0, 1.0, 0.0, 0.0,
            0.0, 0.0, 1.0, 0.0,
            0.0, 0.0, 0.0, 1.0
        );

        public double M11;
        public double M12;
        public double M13;
        public double M14;
        public double M21;
        public double M22;
        public double M23;
        public double M24;
        public double M31;
        public double M32;
        public double M33;
        public double M34;
        public double M41;
        public double M42;
        public double M43;
        public double M44;

        public Matrix4x4(
            double m11, double m12, double m13, double m14,
            double m21, double m22, double m23, double m24,
            double m31, double m32, double m33, double m34,
            double m41, double m42, double m43, double m44)
        {
            M11 = m11;
            M12 = m12;
            M13 = m13;
            M14 = m14;
            M21 = m21;
            M22 = m22;
            M23 = m23;
            M24 = m24;
            M31 = m31;
            M32 = m32;
            M33 = m33;
            M34 = m34;
            M41 = m41;
            M42 = m42;
            M43 = m43;
            M44 = m44;
        }

        public static Matrix4x4 CreateFromEuler(in Vector3 euler, RotationOrder order = RotationOrder.XYZ)
        {
            const double kToRad = Math.PI / 180.0;

            var rx = CreateRotationX(euler.X * kToRad);
            var ry = CreateRotationY(euler.Y * kToRad);
            var rz = CreateRotationZ(euler.Z * kToRad);

            switch (order)
            {
                case RotationOrder.XYZ: return rz * ry * rx;
                case RotationOrder.XZY: return ry * rz * rx;
                case RotationOrder.YXZ: return rz * rx * ry;
                case RotationOrder.YZX: return rx * rz * ry;
                case RotationOrder.ZXY: return ry * rx * rz;
                case RotationOrder.ZYX: return rx * ry * rz;
                default: return rx * ry * rz;
            }
        }

        public static Matrix4x4 CreateRotationX(double radians)
        {
            var cos = Math.Cos(radians);
            var sin = Math.Sin(radians);

            return new Matrix4x4
            (
                1.0, 0.0, 0.0, 0.0,
                0.0, +cos, sin, 0.0,
                0.0, -sin, cos, 0.0,
                0.0, 0.0, 0.0, 1.0
            );
        }

        public static Matrix4x4 CreateRotationY(double radians)
        {
            var cos = Math.Cos(radians);
            var sin = Math.Sin(radians);

            return new Matrix4x4
            (
                cos, 0.0, -sin, 0.0,
                0.0, 1.0, 0.0, 0.0,
                sin, 0.0, cos, 0.0,
                0.0, 0.0, 0.0, 1.0
            );
        }

        public static Matrix4x4 CreateRotationZ(double radians)
        {
            var cos = Math.Cos(radians);
            var sin = Math.Sin(radians);

            return new Matrix4x4
            (
                cos, sin, 0.0, 0.0,
                -sin, cos, 0.0, 0.0,
                0.0, 0.0, 1.0, 0.0,
                0.0, 0.0, 0.0, 1.0
            );
        }

        public static Matrix4x4 CreateScale(in Vector3 scale)
        {
            return new Matrix4x4
            (
                scale.X, 0.0, 0.0, 0.0,
                0.0, scale.Y, 0.0, 0.0,
                0.0, 0.0, scale.Z, 0.0,
                0.0, 0.0, 0.0, 1.0
            );
        }

        public static Matrix4x4 CreateTranslation(in Vector3 translation)
        {
            return new Matrix4x4
            (
                1.0, 0.0, 0.0, 0.0,
                0.0, 1.0, 0.0, 0.0,
                0.0, 0.0, 1.0, 0.0,
                translation.X, translation.Y, translation.Z, 1.0
            );
        }

        public static Matrix4x4 operator *(in Matrix4x4 a, in Matrix4x4 b)
        {
            return new Matrix4x4
            (
                b.M11 * a.M11 + b.M12 * a.M21 + b.M13 * a.M31 + b.M14 * a.M41,
                b.M11 * a.M12 + b.M12 * a.M22 + b.M13 * a.M32 + b.M14 * a.M42,
                b.M11 * a.M13 + b.M12 * a.M23 + b.M13 * a.M33 + b.M14 * a.M43,
                b.M11 * a.M14 + b.M12 * a.M24 + b.M13 * a.M34 + b.M14 * a.M44,
                b.M21 * a.M11 + b.M22 * a.M21 + b.M23 * a.M31 + b.M24 * a.M41,
                b.M21 * a.M12 + b.M22 * a.M22 + b.M23 * a.M32 + b.M24 * a.M42,
                b.M21 * a.M13 + b.M22 * a.M23 + b.M23 * a.M33 + b.M24 * a.M43,
                b.M21 * a.M14 + b.M22 * a.M24 + b.M23 * a.M34 + b.M24 * a.M44,
                b.M31 * a.M11 + b.M32 * a.M21 + b.M33 * a.M31 + b.M34 * a.M41,
                b.M31 * a.M12 + b.M32 * a.M22 + b.M33 * a.M32 + b.M34 * a.M42,
                b.M31 * a.M13 + b.M32 * a.M23 + b.M33 * a.M33 + b.M34 * a.M43,
                b.M31 * a.M14 + b.M32 * a.M24 + b.M33 * a.M34 + b.M34 * a.M44,
                b.M41 * a.M11 + b.M42 * a.M21 + b.M43 * a.M31 + b.M44 * a.M41,
                b.M41 * a.M12 + b.M42 * a.M22 + b.M43 * a.M32 + b.M44 * a.M42,
                b.M41 * a.M13 + b.M42 * a.M23 + b.M43 * a.M33 + b.M44 * a.M43,
                b.M41 * a.M14 + b.M42 * a.M24 + b.M43 * a.M34 + b.M44 * a.M44
            );
        }

        public Vector4 GetRow(int index)
        {
            switch (index)
            {
                case 1: return new Vector4(M11, M12, M13, M14);
                case 2: return new Vector4(M21, M22, M23, M24);
                case 3: return new Vector4(M31, M32, M33, M34);
                case 4: return new Vector4(M41, M42, M43, M44);
                default: return default;
            }
        }

        public bool Equals(Matrix4x4 matrix)
        {
            return
                M11 == matrix.M11 &&
                M12 == matrix.M12 &&
                M13 == matrix.M13 &&
                M14 == matrix.M14 &&
                M21 == matrix.M21 &&
                M22 == matrix.M22 &&
                M23 == matrix.M23 &&
                M24 == matrix.M24 &&
                M31 == matrix.M31 &&
                M32 == matrix.M32 &&
                M33 == matrix.M33 &&
                M34 == matrix.M34 &&
                M41 == matrix.M41 &&
                M42 == matrix.M42 &&
                M43 == matrix.M43 &&
                M44 == matrix.M44;
        }

        public override bool Equals(object obj)
        {
            return obj is Matrix4x4 matrix && Equals(matrix);
        }

        public override int GetHashCode()
        {
            return (GetRow(1), GetRow(2), GetRow(3), GetRow(4)).GetHashCode();
        }

        public static bool operator ==(in Matrix4x4 lhs, in Matrix4x4 rhs)
        {
            return lhs.Equals(rhs);
        }

        public static bool operator !=(in Matrix4x4 lhs, in Matrix4x4 rhs)
        {
            return !lhs.Equals(rhs);
        }
    }
}