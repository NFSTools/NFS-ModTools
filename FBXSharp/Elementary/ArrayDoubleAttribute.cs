using System;
using System.Diagnostics;
using FBXSharp.Core;

namespace FBXSharp.Elementary
{
    [DebuggerDisplay("Double : {this.Value.Length} Items")]
    public class ArrayDoubleAttribute : IGenericAttribute<double[]>
    {
        public static readonly IElementAttributeType PropertyType = IElementAttributeType.ArrayDouble;

        public static readonly int PropertyStride = 8;

        public static readonly int PropertyLength = -1;

        public IElementAttributeType Type => PropertyType;

        public int Size => 12 + PropertyStride * Length;

        public int Stride => PropertyStride;

        public int Length { get; }

        public double[] Value { get; }

        public ArrayDoubleAttribute(double[] value)
        {
            Value = value ?? Array.Empty<double>();
            Length = Value.Length;
        }

        public object GetElementValue()
        {
            return Value;
        }
    }
}