namespace FBXSharp.Core
{
    public enum IElementAttributeType : byte
    {
        Byte = (byte)'C',
        Int16 = (byte)'Y',
        Int32 = (byte)'I',
        Int64 = (byte)'L',
        Single = (byte)'F',
        Double = (byte)'D',
        String = (byte)'S',
        ArrayBoolean = (byte)'b',
        ArrayInt32 = (byte)'i',
        ArrayInt64 = (byte)'l',
        ArraySingle = (byte)'f',
        ArrayDouble = (byte)'d',
        Binary = (byte)'R'
    }

    public interface IElementAttribute
    {
        IElementAttributeType Type { get; }
        int Stride { get; }
        int Length { get; }
        int Size { get; }

        object GetElementValue();
    }

    public interface IGenericAttribute<T> : IElementAttribute
    {
        T Value { get; }
    }
}