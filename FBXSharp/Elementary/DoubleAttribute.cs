using System.Diagnostics;
using FBXSharp.Core;

namespace FBXSharp.Elementary
{
    [DebuggerDisplay("{this.Value}D")]
    public class DoubleAttribute : IGenericAttribute<double>
    {
        public static readonly IElementAttributeType PropertyType = IElementAttributeType.Double;

        public static readonly int PropertyStride = 8;

        public static readonly int PropertyLength = 1;

        public IElementAttributeType Type => PropertyType;

        public int Size => PropertyStride;

        public int Stride => PropertyStride;

        public int Length => PropertyLength;

        public double Value { get; }

        public DoubleAttribute(double value)
        {
            Value = value;
        }

        public object GetElementValue()
        {
            return Value;
        }
    }
}