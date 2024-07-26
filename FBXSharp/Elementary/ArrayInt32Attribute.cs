using System;
using System.Diagnostics;
using FBXSharp.Core;

namespace FBXSharp.Elementary
{
    [DebuggerDisplay("Int32 : {this.Value.Length} Items")]
    public class ArrayInt32Attribute : IGenericAttribute<int[]>
    {
        public static readonly IElementAttributeType PropertyType = IElementAttributeType.ArrayInt32;

        public static readonly int PropertyStride = 4;

        public static readonly int PropertyLength = -1;

        public IElementAttributeType Type => PropertyType;

        public int Size => 12 + PropertyStride * Length;

        public int Stride => PropertyStride;

        public int Length { get; }

        public int[] Value { get; }

        public ArrayInt32Attribute(int[] value)
        {
            Value = value ?? Array.Empty<int>();
            Length = Value.Length;
        }

        public object GetElementValue()
        {
            return Value;
        }
    }
}