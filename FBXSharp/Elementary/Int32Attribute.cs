using System.Diagnostics;
using FBXSharp.Core;

namespace FBXSharp.Elementary
{
    [DebuggerDisplay("{this.Value}")]
    public class Int32Attribute : IGenericAttribute<int>
    {
        public static readonly IElementAttributeType PropertyType = IElementAttributeType.Int32;

        public static readonly int PropertyStride = 4;

        public static readonly int PropertyLength = 1;

        public IElementAttributeType Type => PropertyType;

        public int Size => PropertyStride;

        public int Stride => PropertyStride;

        public int Length => PropertyLength;

        public int Value { get; }

        public Int32Attribute(int value)
        {
            Value = value;
        }

        public object GetElementValue()
        {
            return Value;
        }
    }
}