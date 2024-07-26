using System.Diagnostics;
using FBXSharp.Core;

namespace FBXSharp.Elementary
{
    [DebuggerDisplay("{this.Value}S")]
    public class Int16Attribute : IGenericAttribute<short>
    {
        public static readonly IElementAttributeType PropertyType = IElementAttributeType.Int16;

        public static readonly int PropertyStride = 2;

        public static readonly int PropertyLength = 1;

        public IElementAttributeType Type => PropertyType;

        public int Size => PropertyStride;

        public int Stride => PropertyStride;

        public int Length => PropertyLength;

        public short Value { get; }

        public Int16Attribute(short value)
        {
            Value = value;
        }

        public object GetElementValue()
        {
            return Value;
        }
    }
}