using System.Runtime.InteropServices;

namespace FBXSharp.ValueTypes
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Vector2
    {
        public const int SizeOf = 0x10;

        public double X;
        public double Y;

        public Vector2(double x, double y)
        {
            X = x;
            Y = y;
        }

        public static Vector2 operator -(in Vector2 vector)
        {
            return new Vector2(-vector.X, -vector.Y);
        }

        public static Vector2 operator +(in Vector2 a, in Vector2 b)
        {
            return new Vector2(a.X + b.X, a.Y + b.Y);
        }

        public static Vector2 operator -(in Vector2 a, in Vector2 b)
        {
            return new Vector2(a.X - b.X, a.Y - b.Y);
        }

        public static bool operator ==(in Vector2 lhs, in Vector2 rhs)
        {
            return lhs.X == rhs.X && lhs.Y == rhs.Y;
        }

        public static bool operator !=(in Vector2 lhs, in Vector2 rhs)
        {
            return lhs.X != rhs.X || lhs.Y != rhs.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is Vector2 vector && this == vector;
        }

        public override int GetHashCode()
        {
            return (X, Y).GetHashCode();
        }

        public override string ToString()
        {
            return $"<{X}, {Y}>";
        }
    }
}