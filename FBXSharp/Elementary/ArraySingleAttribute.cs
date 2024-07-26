using System;
using System.Diagnostics;
using FBXSharp.Core;

namespace FBXSharp.Elementary
{
    [DebuggerDisplay("Single : {this.Value.Length} Items")]
    public class ArraySingleAttribute : IGenericAttribute<float[]>
    {
        public static readonly IElementAttributeType PropertyType = IElementAttributeType.ArraySingle;

        public static readonly int PropertyStride = 4;

        public static readonly int PropertyLength = -1;

        public IElementAttributeType Type => PropertyType;

        public int Size => 12 + PropertyStride * Length;

        public int Stride => PropertyStride;

        public int Length { get; }

        public float[] Value { get; }

        public ArraySingleAttribute(float[] value)
        {
            Value = value ?? Array.Empty<float>();
            Length = Value.Length;
        }

        public object GetElementValue()
        {
            return Value;
        }
    }
}