using System.Diagnostics;
using FBXSharp.Core;

namespace FBXSharp.Elementary
{
    [DebuggerDisplay("{this.Value}F")]
    public class SingleAttribute : IGenericAttribute<float>
    {
        public static readonly IElementAttributeType PropertyType = IElementAttributeType.Single;

        public static readonly int PropertyStride = 4;

        public static readonly int PropertyLength = 1;

        public IElementAttributeType Type => PropertyType;

        public int Size => PropertyStride;

        public int Stride => PropertyStride;

        public int Length => PropertyLength;

        public float Value { get; }

        public SingleAttribute(float value)
        {
            Value = value;
        }

        public object GetElementValue()
        {
            return Value;
        }
    }
}