using System;
using FBXSharp.Core;
using FBXSharp.Elementary;
using FBXSharp.ValueTypes;

namespace FBXSharp
{
    internal static class ElementaryFactory
    {
        public static S[] DeepGenericCopy<T, S>(T[] buffer)
        {
            var array = new S[buffer.Length];

            Array.Copy(buffer, array, array.Length);

            return array;
        }

        private static Vector2[] DeepVector2Copy<T>(T[] buffer)
        {
            var array = new Vector2[buffer.Length >> 1];

            for (int i = 0, k = 0; i < array.Length; ++i, k += 2)
                array[i] = new Vector2
                (
                    Convert.ToDouble(buffer[k + 0]),
                    Convert.ToDouble(buffer[k + 1])
                );

            return array;
        }

        private static Vector3[] DeepVector3Copy<T>(T[] buffer)
        {
            var array = new Vector3[buffer.Length / 3];

            for (int i = 0, k = 0; i < array.Length; ++i, k += 3)
                array[i] = new Vector3
                (
                    Convert.ToDouble(buffer[k + 0]),
                    Convert.ToDouble(buffer[k + 1]),
                    Convert.ToDouble(buffer[k + 2])
                );

            return array;
        }

        private static Vector4[] DeepVector4Copy<T>(T[] buffer)
        {
            var array = new Vector4[buffer.Length >> 2];

            for (int i = 0, k = 0; i < array.Length; ++i, k += 4)
                array[i] = new Vector4
                (
                    Convert.ToDouble(buffer[k + 0]),
                    Convert.ToDouble(buffer[k + 1]),
                    Convert.ToDouble(buffer[k + 2]),
                    Convert.ToDouble(buffer[k + 3])
                );

            return array;
        }

        public static double[] VtoDArray<TFrom, TTo>(TFrom[] from)
            where TFrom : unmanaged
            where TTo : unmanaged
        {
            unsafe
            {
                var length = sizeof(TTo) / sizeof(double) * from.Length;
                var buffer = new double[length];

                fixed (TFrom* frmPtr = &from[0])
                fixed (double* toPtr = &buffer[0])
                {
                    var ptr = (TTo*)toPtr;

                    for (var i = 0; i < from.Length; ++i) *(ptr + i) = *(TTo*)(frmPtr + i);
                }

                return buffer;
            }
        }

        public static bool ToInt32(IElementAttribute attribute, out int value)
        {
            switch (attribute.Type)
            {
                case IElementAttributeType.Byte:
                case IElementAttributeType.Int16:
                case IElementAttributeType.Int32:
                case IElementAttributeType.Int64:
                case IElementAttributeType.Single:
                case IElementAttributeType.Double:
                    value = Convert.ToInt32(attribute.GetElementValue());
                    return true;

                case IElementAttributeType.ArrayBoolean:
                case IElementAttributeType.ArrayInt32:
                case IElementAttributeType.ArrayInt64:
                case IElementAttributeType.ArraySingle:
                case IElementAttributeType.ArrayDouble:
                    var buffer = attribute.GetElementValue() as Array;
                    var result = buffer?.Length > 0;
                    value = result ? Convert.ToInt32(buffer.GetValue(0)) : 0;
                    return result;

                default:
                    value = 0;
                    return false;
            }
        }

        public static bool ToDouble(IElementAttribute attribute, out double value)
        {
            switch (attribute.Type)
            {
                case IElementAttributeType.Byte:
                case IElementAttributeType.Int16:
                case IElementAttributeType.Int32:
                case IElementAttributeType.Int64:
                case IElementAttributeType.Single:
                case IElementAttributeType.Double:
                    value = Convert.ToDouble(attribute.GetElementValue());
                    return true;

                case IElementAttributeType.ArrayBoolean:
                case IElementAttributeType.ArrayInt32:
                case IElementAttributeType.ArrayInt64:
                case IElementAttributeType.ArraySingle:
                case IElementAttributeType.ArrayDouble:
                    var buffer = attribute.GetElementValue() as Array;
                    var result = buffer?.Length > 0;
                    value = result ? Convert.ToDouble(buffer.GetValue(0)) : 0.0;
                    return result;

                default:
                    value = 0.0;
                    return false;
            }
        }

        public static bool ToVector2(IElementAttribute attribute, out Vector2 vector)
        {
            Array array;

            switch (attribute.Type)
            {
                case IElementAttributeType.ArrayBoolean:
                case IElementAttributeType.ArrayInt32:
                case IElementAttributeType.ArrayInt64:
                case IElementAttributeType.ArraySingle:
                case IElementAttributeType.ArrayDouble:
                    array = attribute.GetElementValue() as Array;
                    break;

                default:
                    vector = default;
                    return false;
            }

            if (array.Length < 2)
            {
                vector = default;
                return false;
            }

            vector = new Vector2
            (
                Convert.ToDouble(array.GetValue(0)),
                Convert.ToDouble(array.GetValue(1))
            );

            return true;
        }

        public static bool ToVector3(IElementAttribute attribute, out Vector3 vector)
        {
            Array array;

            switch (attribute.Type)
            {
                case IElementAttributeType.ArrayBoolean:
                case IElementAttributeType.ArrayInt32:
                case IElementAttributeType.ArrayInt64:
                case IElementAttributeType.ArraySingle:
                case IElementAttributeType.ArrayDouble:
                    array = attribute.GetElementValue() as Array;
                    break;

                default:
                    vector = default;
                    return false;
            }

            if (array.Length < 3)
            {
                vector = default;
                return false;
            }

            vector = new Vector3
            (
                Convert.ToDouble(array.GetValue(0)),
                Convert.ToDouble(array.GetValue(1)),
                Convert.ToDouble(array.GetValue(2))
            );

            return true;
        }

        public static bool ToVector4(IElementAttribute attribute, out Vector4 vector)
        {
            Array array;

            switch (attribute.Type)
            {
                case IElementAttributeType.ArrayBoolean:
                case IElementAttributeType.ArrayInt32:
                case IElementAttributeType.ArrayInt64:
                case IElementAttributeType.ArraySingle:
                case IElementAttributeType.ArrayDouble:
                    array = attribute.GetElementValue() as Array;
                    break;

                default:
                    vector = default;
                    return false;
            }

            if (array.Length < 4)
            {
                vector = default;
                return false;
            }

            vector = new Vector4
            (
                Convert.ToDouble(array.GetValue(0)),
                Convert.ToDouble(array.GetValue(1)),
                Convert.ToDouble(array.GetValue(2)),
                Convert.ToDouble(array.GetValue(3))
            );

            return true;
        }

        public static bool ToMatrix4x4(IElementAttribute attribute, out Matrix4x4 matrix)
        {
            Array array;

            switch (attribute.Type)
            {
                case IElementAttributeType.ArrayBoolean:
                case IElementAttributeType.ArrayInt32:
                case IElementAttributeType.ArrayInt64:
                case IElementAttributeType.ArraySingle:
                case IElementAttributeType.ArrayDouble:
                    array = attribute.GetElementValue() as Array;
                    break;

                default:
                    matrix = default;
                    return false;
            }

            if (array.Length < 16)
            {
                matrix = default;
                return false;
            }

            matrix = new Matrix4x4
            (
                Convert.ToDouble(array.GetValue(0)),
                Convert.ToDouble(array.GetValue(1)),
                Convert.ToDouble(array.GetValue(2)),
                Convert.ToDouble(array.GetValue(3)),
                Convert.ToDouble(array.GetValue(4)),
                Convert.ToDouble(array.GetValue(5)),
                Convert.ToDouble(array.GetValue(6)),
                Convert.ToDouble(array.GetValue(7)),
                Convert.ToDouble(array.GetValue(8)),
                Convert.ToDouble(array.GetValue(9)),
                Convert.ToDouble(array.GetValue(10)),
                Convert.ToDouble(array.GetValue(11)),
                Convert.ToDouble(array.GetValue(12)),
                Convert.ToDouble(array.GetValue(13)),
                Convert.ToDouble(array.GetValue(14)),
                Convert.ToDouble(array.GetValue(15))
            );

            return true;
        }

        public static bool ToInt32Array(IElementAttribute attribute, out int[] array)
        {
            switch (attribute.Type)
            {
                case IElementAttributeType.ArrayBoolean:
                {
                    array = DeepGenericCopy<bool, int>(attribute.GetElementValue() as bool[]);
                    return true;
                }

                case IElementAttributeType.ArrayDouble:
                {
                    array = DeepGenericCopy<double, int>(attribute.GetElementValue() as double[]);
                    return true;
                }

                case IElementAttributeType.ArrayInt32:
                {
                    array = DeepGenericCopy<int, int>(attribute.GetElementValue() as int[]);
                    return true;
                }

                case IElementAttributeType.ArrayInt64:
                {
                    array = DeepGenericCopy<long, int>(attribute.GetElementValue() as long[]);
                    return true;
                }

                case IElementAttributeType.ArraySingle:
                {
                    array = DeepGenericCopy<float, int>(attribute.GetElementValue() as float[]);
                    return true;
                }

                default:
                {
                    array = null;
                    return false;
                }
            }
        }

        public static bool ToDoubleArray(IElementAttribute attribute, out double[] array)
        {
            switch (attribute.Type)
            {
                case IElementAttributeType.ArrayBoolean:
                {
                    array = DeepGenericCopy<bool, double>(attribute.GetElementValue() as bool[]);
                    return true;
                }

                case IElementAttributeType.ArrayDouble:
                {
                    array = DeepGenericCopy<double, double>(attribute.GetElementValue() as double[]);
                    return true;
                }

                case IElementAttributeType.ArrayInt32:
                {
                    array = DeepGenericCopy<int, double>(attribute.GetElementValue() as int[]);
                    return true;
                }

                case IElementAttributeType.ArrayInt64:
                {
                    array = DeepGenericCopy<long, double>(attribute.GetElementValue() as long[]);
                    return true;
                }

                case IElementAttributeType.ArraySingle:
                {
                    array = DeepGenericCopy<float, double>(attribute.GetElementValue() as float[]);
                    return true;
                }

                default:
                {
                    array = null;
                    return false;
                }
            }
        }

        public static bool ToVector2Array(IElementAttribute attribute, out Vector2[] array)
        {
            switch (attribute.Type)
            {
                case IElementAttributeType.ArrayBoolean:
                {
                    array = DeepVector2Copy(attribute.GetElementValue() as bool[]);
                    return true;
                }

                case IElementAttributeType.ArrayDouble:
                {
                    array = DeepVector2Copy(attribute.GetElementValue() as double[]);
                    return true;
                }

                case IElementAttributeType.ArrayInt32:
                {
                    array = DeepVector2Copy(attribute.GetElementValue() as int[]);
                    return true;
                }

                case IElementAttributeType.ArrayInt64:
                {
                    array = DeepVector2Copy(attribute.GetElementValue() as long[]);
                    return true;
                }

                case IElementAttributeType.ArraySingle:
                {
                    array = DeepVector2Copy(attribute.GetElementValue() as float[]);
                    return true;
                }

                default:
                {
                    array = null;
                    return false;
                }
            }
        }

        public static bool ToVector3Array(IElementAttribute attribute, out Vector3[] array)
        {
            switch (attribute.Type)
            {
                case IElementAttributeType.ArrayBoolean:
                {
                    array = DeepVector3Copy(attribute.GetElementValue() as bool[]);
                    return true;
                }

                case IElementAttributeType.ArrayDouble:
                {
                    array = DeepVector3Copy(attribute.GetElementValue() as double[]);
                    return true;
                }

                case IElementAttributeType.ArrayInt32:
                {
                    array = DeepVector3Copy(attribute.GetElementValue() as int[]);
                    return true;
                }

                case IElementAttributeType.ArrayInt64:
                {
                    array = DeepVector3Copy(attribute.GetElementValue() as long[]);
                    return true;
                }

                case IElementAttributeType.ArraySingle:
                {
                    array = DeepVector3Copy(attribute.GetElementValue() as float[]);
                    return true;
                }

                default:
                {
                    array = null;
                    return false;
                }
            }
        }

        public static bool ToVector4Array(IElementAttribute attribute, out Vector4[] array)
        {
            switch (attribute.Type)
            {
                case IElementAttributeType.ArrayBoolean:
                {
                    array = DeepVector4Copy(attribute.GetElementValue() as bool[]);
                    return true;
                }

                case IElementAttributeType.ArrayDouble:
                {
                    array = DeepVector4Copy(attribute.GetElementValue() as double[]);
                    return true;
                }

                case IElementAttributeType.ArrayInt32:
                {
                    array = DeepVector4Copy(attribute.GetElementValue() as int[]);
                    return true;
                }

                case IElementAttributeType.ArrayInt64:
                {
                    array = DeepVector4Copy(attribute.GetElementValue() as long[]);
                    return true;
                }

                case IElementAttributeType.ArraySingle:
                {
                    array = DeepVector4Copy(attribute.GetElementValue() as float[]);
                    return true;
                }

                default:
                {
                    array = null;
                    return false;
                }
            }
        }

        public static Vector4 ExtendVector(in Vector3 vector)
        {
            return new Vector4(vector.X, vector.Y, vector.Z);
        }

        public static Vector4 ExtendVector(in Vector3 vector, double w)
        {
            return new Vector4(vector.X, vector.Y, vector.Z, w);
        }

        public static IElementAttribute GetElementAttribute(object value)
        {
            var type = value.GetType();

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean: return GetElementAttribute((bool)value);
                case TypeCode.Char: return GetElementAttribute((char)value);
                case TypeCode.SByte: return GetElementAttribute((sbyte)value);
                case TypeCode.Byte: return GetElementAttribute((byte)value);
                case TypeCode.Int16: return GetElementAttribute((short)value);
                case TypeCode.UInt16: return GetElementAttribute((ushort)value);
                case TypeCode.Int32: return GetElementAttribute((int)value);
                case TypeCode.UInt32: return GetElementAttribute((uint)value);
                case TypeCode.Int64: return GetElementAttribute((long)value);
                case TypeCode.UInt64: return GetElementAttribute((ulong)value);
                case TypeCode.Single: return GetElementAttribute((float)value);
                case TypeCode.Double: return GetElementAttribute((double)value);
                case TypeCode.String: return GetElementAttribute(value.ToString());
            }

            if (type.IsArray)
                switch (Type.GetTypeCode(type.GetElementType()))
                {
                    case TypeCode.Boolean: return GetElementAttribute(value as bool[]);
                    case TypeCode.Char: return GetElementAttribute(value as char[]);
                    case TypeCode.SByte: return GetElementAttribute(value as sbyte[]);
                    case TypeCode.Byte: return GetElementAttribute(value as byte[]);
                    case TypeCode.Int16: return GetElementAttribute(value as short[]);
                    case TypeCode.UInt16: return GetElementAttribute(value as ushort[]);
                    case TypeCode.Int32: return GetElementAttribute(value as int[]);
                    case TypeCode.UInt32: return GetElementAttribute(value as uint[]);
                    case TypeCode.Int64: return GetElementAttribute(value as long[]);
                    case TypeCode.UInt64: return GetElementAttribute(value as ulong[]);
                    case TypeCode.Single: return GetElementAttribute(value as float[]);
                    case TypeCode.Double: return GetElementAttribute(value as double[]);
                }

            if (value is Vector2 vector2) return GetElementAttribute(vector2);

            if (value is Vector3 vector3) return GetElementAttribute(vector3);

            if (value is Vector4 vector4) return GetElementAttribute(vector4);

            if (value is Matrix4x4 matrix) return GetElementAttribute(matrix);

            return GetElementAttribute(value.ToString());
        }

        public static IElementAttribute GetElementAttribute(bool value)
        {
            return new Int32Attribute(value ? 1 : 0);
        }

        public static IElementAttribute GetElementAttribute(byte value)
        {
            return new ByteAttribute(value);
        }

        public static IElementAttribute GetElementAttribute(sbyte value)
        {
            return new ByteAttribute((byte)value);
        }

        public static IElementAttribute GetElementAttribute(char value)
        {
            return new Int16Attribute((short)value);
        }

        public static IElementAttribute GetElementAttribute(short value)
        {
            return new Int16Attribute(value);
        }

        public static IElementAttribute GetElementAttribute(ushort value)
        {
            return new Int16Attribute((short)value);
        }

        public static IElementAttribute GetElementAttribute(int value)
        {
            return new Int32Attribute(value);
        }

        public static IElementAttribute GetElementAttribute(uint value)
        {
            return new Int32Attribute((int)value);
        }

        public static IElementAttribute GetElementAttribute(long value)
        {
            return new Int64Attribute(value);
        }

        public static IElementAttribute GetElementAttribute(ulong value)
        {
            return new Int64Attribute((long)value);
        }

        public static IElementAttribute GetElementAttribute(float value)
        {
            return new SingleAttribute(value);
        }

        public static IElementAttribute GetElementAttribute(double value)
        {
            return new DoubleAttribute(value);
        }

        public static IElementAttribute GetElementAttribute(string value)
        {
            return new StringAttribute(value);
        }

        public static IElementAttribute GetElementAttribute(bool[] value)
        {
            return new ArrayBooleanAttribute(value);
        }

        public static IElementAttribute GetElementAttribute(byte[] value)
        {
            return new BinaryAttribute(value);
        }

        public static IElementAttribute GetElementAttribute(sbyte[] value)
        {
            return new BinaryAttribute(DeepGenericCopy<sbyte, byte>(value));
        }

        public static IElementAttribute GetElementAttribute(char[] value)
        {
            return new ArrayInt32Attribute(DeepGenericCopy<char, int>(value));
        }

        public static IElementAttribute GetElementAttribute(short[] value)
        {
            return new ArrayInt32Attribute(DeepGenericCopy<short, int>(value));
        }

        public static IElementAttribute GetElementAttribute(ushort[] value)
        {
            return new ArrayInt32Attribute(DeepGenericCopy<ushort, int>(value));
        }

        public static IElementAttribute GetElementAttribute(int[] value)
        {
            return new ArrayInt32Attribute(value);
        }

        public static IElementAttribute GetElementAttribute(uint[] value)
        {
            return new ArrayInt32Attribute(DeepGenericCopy<uint, int>(value));
        }

        public static IElementAttribute GetElementAttribute(long[] value)
        {
            return new ArrayInt64Attribute(value);
        }

        public static IElementAttribute GetElementAttribute(ulong[] value)
        {
            return new ArrayInt64Attribute(DeepGenericCopy<ulong, long>(value));
        }

        public static IElementAttribute GetElementAttribute(float[] value)
        {
            return new ArraySingleAttribute(value);
        }

        public static IElementAttribute GetElementAttribute(double[] value)
        {
            return new ArrayDoubleAttribute(value);
        }

        public static IElementAttribute GetElementAttribute(in Vector2 value)
        {
            return new ArrayDoubleAttribute(new[]
            {
                value.X, value.Y
            });
        }

        public static IElementAttribute GetElementAttribute(in Vector3 value)
        {
            return new ArrayDoubleAttribute(new[]
            {
                value.X, value.Y, value.Z
            });
        }

        public static IElementAttribute GetElementAttribute(in Vector4 value)
        {
            return new ArrayDoubleAttribute(new[]
            {
                value.X, value.Y, value.Z, value.W
            });
        }

        public static IElementAttribute GetElementAttribute(in Matrix4x4 value)
        {
            return new ArrayDoubleAttribute(new[]
            {
                value.M11, value.M12, value.M13, value.M14,
                value.M21, value.M22, value.M23, value.M24,
                value.M31, value.M32, value.M33, value.M34,
                value.M41, value.M42, value.M43, value.M44
            });
        }
    }
}