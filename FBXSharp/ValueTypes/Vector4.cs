using System.Runtime.InteropServices;

namespace FBXSharp.ValueTypes
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Vector4
    {
        public const int SizeOf = 0x20;

        public double X;
        public double Y;
        public double Z;
        public double W;

        public Vector4(double x, double y, double z, double w = 1.0)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public static Vector4 operator -(in Vector4 vector)
        {
            return new Vector4(-vector.X, -vector.Y, -vector.Z, -vector.W);
        }

        public static Vector4 operator +(in Vector4 a, in Vector4 b)
        {
            return new Vector4(a.X + b.X, a.Y + b.Y, a.Z + b.Z, a.W + b.W);
        }

        public static Vector4 operator -(in Vector4 a, in Vector4 b)
        {
            return new Vector4(a.X - b.X, a.Y - b.Y, a.Z - b.Z, a.W - b.W);
        }

        public static bool operator ==(in Vector4 lhs, in Vector4 rhs)
        {
            return lhs.X == rhs.X && lhs.Y == rhs.Y && lhs.Z == rhs.Z && lhs.W == rhs.W;
        }

        public static bool operator !=(in Vector4 lhs, in Vector4 rhs)
        {
            return lhs.X != rhs.X || lhs.Y != rhs.Y || lhs.Z != rhs.Z || lhs.W != rhs.W;
        }

        public override bool Equals(object obj)
        {
            return obj is Vector4 vector && this == vector;
        }

        public override int GetHashCode()
        {
            return (X, Y, Z, W).GetHashCode();
        }

        public override string ToString()
        {
            return $"<{X}, {Y}, {Z}, {W}>";
        }
    }
}