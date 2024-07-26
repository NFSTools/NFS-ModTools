using System.Runtime.InteropServices;

namespace FBXSharp.ValueTypes
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ColorRGBA
    {
        public const int SizeOf = 0x10;

        public double R;
        public double G;
        public double B;
        public double A;

        public ColorRGBA(double r, double g, double b, double a = 1.0)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public ColorRGBA(in Vector3 vector) : this(vector.X, vector.Y, vector.Z)
        {
        }

        public ColorRGBA(in Vector4 vector) : this(vector.X, vector.Y, vector.Z, vector.W)
        {
        }

        public static implicit operator Vector4(in ColorRGBA color)
        {
            return new Vector4(color.R, color.G, color.B, color.A);
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ColorRGB
    {
        public const int SizeOf = 0x18;

        public double R;
        public double G;
        public double B;

        public ColorRGB(double r, double g, double b)
        {
            R = r;
            G = g;
            B = b;
        }

        public ColorRGB(in Vector3 vector) : this(vector.X, vector.Y, vector.Z)
        {
        }

        public ColorRGB(in Vector4 vector) : this(vector.X, vector.Y, vector.Z)
        {
        }

        public static implicit operator Vector3(in ColorRGB color)
        {
            return new Vector3(color.R, color.G, color.B);
        }
    }
}