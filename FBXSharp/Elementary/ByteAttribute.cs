using System.Diagnostics;
using FBXSharp.Core;

namespace FBXSharp.Elementary
{
    [DebuggerDisplay("{this.Value}B")]
    public class ByteAttribute : IGenericAttribute<byte>
    {
        public static readonly IElementAttributeType PropertyType = IElementAttributeType.Byte;

        public static readonly int PropertyStride = 1;

        public static readonly int PropertyLength = 1;

        public IElementAttributeType Type => PropertyType;

        public int Size => PropertyStride;

        public int Stride => PropertyStride;

        public int Length => PropertyLength;

        public byte Value { get; }

        public ByteAttribute(byte value)
        {
            Value = value;
        }

        public object GetElementValue()
        {
            return Value;
        }
    }
}