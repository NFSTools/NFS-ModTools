using System.Diagnostics;
using FBXSharp.Core;

namespace FBXSharp.Elementary
{
    [DebuggerDisplay("{this.Value}L")]
    public class Int64Attribute : IGenericAttribute<long>
    {
        public static readonly IElementAttributeType PropertyType = IElementAttributeType.Int64;

        public static readonly int PropertyStride = 8;

        public static readonly int PropertyLength = 1;

        public IElementAttributeType Type => PropertyType;

        public int Size => PropertyStride;

        public int Stride => PropertyStride;

        public int Length => PropertyLength;

        public long Value { get; }

        public Int64Attribute(long value)
        {
            Value = value;
        }

        public object GetElementValue()
        {
            return Value;
        }
    }
}