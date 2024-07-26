using System.Diagnostics;
using FBXSharp.Core;

namespace FBXSharp.Elementary
{
    [DebuggerDisplay("{this.Value}")]
    public class StringAttribute : IGenericAttribute<string>
    {
        public static readonly IElementAttributeType PropertyType = IElementAttributeType.String;

        public static readonly int PropertyStride = -1;

        public static readonly int PropertyLength = 1;

        public IElementAttributeType Type => PropertyType;

        public int Size => 4 + Stride;

        public int Stride { get; }

        public int Length => PropertyLength;

        public string Value { get; }

        public StringAttribute(string value)
        {
            // 4 bytes char count + string length
            Value = value ?? string.Empty;
            Stride = Value.Length;
        }

        public object GetElementValue()
        {
            return Value;
        }
    }
}