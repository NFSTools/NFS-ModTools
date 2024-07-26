using System;
using System.Diagnostics;
using FBXSharp.Core;

namespace FBXSharp.Elementary
{
    [DebuggerDisplay("Boolean : {this.Value.Length} Items")]
    public class ArrayBooleanAttribute : IGenericAttribute<bool[]>
    {
        public static readonly IElementAttributeType PropertyType = IElementAttributeType.ArrayBoolean;

        public static readonly int PropertyStride = 1;

        public static readonly int PropertyLength = -1;

        public IElementAttributeType Type => PropertyType;

        public int Size => 12 + PropertyStride * Length;

        public int Stride => PropertyStride;

        public int Length { get; }

        public bool[] Value { get; }

        public ArrayBooleanAttribute(bool[] value)
        {
            Value = value ?? Array.Empty<bool>();
            Length = Value.Length;
        }

        public object GetElementValue()
        {
            return Value;
        }
    }
}