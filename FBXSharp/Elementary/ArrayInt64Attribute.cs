using System;
using System.Diagnostics;
using FBXSharp.Core;

namespace FBXSharp.Elementary
{
    [DebuggerDisplay("Int64 : {this.Value.Length} Items")]
    public class ArrayInt64Attribute : IGenericAttribute<long[]>
    {
        public static readonly IElementAttributeType PropertyType = IElementAttributeType.ArrayInt64;

        public static readonly int PropertyStride = 8;

        public static readonly int PropertyLength = -1;

        public IElementAttributeType Type => PropertyType;

        public int Size => 12 + PropertyStride * Length;

        public int Stride => PropertyStride;

        public int Length { get; }

        public long[] Value { get; }

        public ArrayInt64Attribute(long[] value)
        {
            Value = value ?? Array.Empty<long>();
            Length = Value.Length;
        }

        public object GetElementValue()
        {
            return Value;
        }
    }
}