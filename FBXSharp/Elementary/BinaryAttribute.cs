using System;
using System.Diagnostics;
using FBXSharp.Core;

namespace FBXSharp.Elementary
{
    [DebuggerDisplay("Raw : {this.Value.Length} bytes")]
    public class BinaryAttribute : IGenericAttribute<byte[]>
    {
        public static readonly IElementAttributeType PropertyType = IElementAttributeType.Binary;

        public static readonly int PropertyStride = 1;

        public static readonly int PropertyLength = -1;

        public IElementAttributeType Type => PropertyType;

        public int Size => 4 + PropertyStride * Length;

        public int Stride => PropertyStride;

        public int Length { get; }

        public byte[] Value { get; }

        public BinaryAttribute(byte[] value)
        {
            Value = value ?? Array.Empty<byte>();
            Length = Value.Length;
        }

        public object GetElementValue()
        {
            return Value;
        }
    }
}