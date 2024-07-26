using System;
using System.Collections.Generic;
using FBXSharp.Core;
using FBXSharp.ValueTypes;

namespace FBXSharp
{
    internal static class PropertyFactory
    {
        public delegate IElementProperty AttributeCreator(IElement element);

        private static readonly Dictionary<string, IElementPropertyType> ms_typeMapper =
            new Dictionary<string, IElementPropertyType>
            {
                { "Byte", IElementPropertyType.SByte },
                { "UByte", IElementPropertyType.Byte },
                { "Short", IElementPropertyType.Short },
                { "UShort", IElementPropertyType.UShort },
                { "UInteger", IElementPropertyType.UInt },
                { "LongLong", IElementPropertyType.Long },
                { "ULongLong", IElementPropertyType.ULong },
                { "HalfFloat", IElementPropertyType.Half },
                { "Bool", IElementPropertyType.Bool },
                { "bool", IElementPropertyType.Bool },
                { "Integer", IElementPropertyType.Int },
                { "int", IElementPropertyType.Int },
                { "Float", IElementPropertyType.Float },
                { "float", IElementPropertyType.Float },
                { "Number", IElementPropertyType.Double },
                { "double", IElementPropertyType.Double },
                { "Vector2", IElementPropertyType.Double2 },
                { "Vector2D", IElementPropertyType.Double2 },
                { "Vector", IElementPropertyType.Double3 },
                { "Vector3D", IElementPropertyType.Double3 },
                { "Vector4", IElementPropertyType.Double4 },
                { "Vector4D", IElementPropertyType.Double4 },
                { "Matrix", IElementPropertyType.Double4x4 },
                { "matrix4x4", IElementPropertyType.Double4x4 },
                { "Enum", IElementPropertyType.Enum },
                { "enum", IElementPropertyType.Enum },
                { "stringlist", IElementPropertyType.Enum },
                { "Time", IElementPropertyType.Time },
                { "KTime", IElementPropertyType.Time },
                { "TimeCode", IElementPropertyType.Double3 },
                { "KTimeCode", IElementPropertyType.Double3 },
                { "Reference", IElementPropertyType.Reference },
                { "ReferenceProperty", IElementPropertyType.Reference },
                { "object", IElementPropertyType.Undefined },
                { "KString", IElementPropertyType.String },
                { "charptr", IElementPropertyType.String },
                { "Action", IElementPropertyType.Bool },
                { "event", IElementPropertyType.Undefined },
                { "Compound", IElementPropertyType.Undefined },
                { "Blob", IElementPropertyType.Blob },
                { "Distance", IElementPropertyType.Distance },
                { "DateTime", IElementPropertyType.DateTime },
                { "Color", IElementPropertyType.Double3 },
                { "ColorRGB", IElementPropertyType.Double3 },
                { "ColorAndAlpha", IElementPropertyType.Double4 },
                { "ColorRGBA", IElementPropertyType.Double4 },
                { "Real", IElementPropertyType.Double },
                { "Translation", IElementPropertyType.Double3 },
                { "Rotation", IElementPropertyType.Double3 },
                { "Scaling", IElementPropertyType.Double3 },
                { "Quaternion", IElementPropertyType.Double4 },
                { "Lcl Translation", IElementPropertyType.Double3 },
                { "Lcl Rotation", IElementPropertyType.Double3 },
                { "Lcl Scaling", IElementPropertyType.Double3 },
                { "Lcl Quaternion", IElementPropertyType.Double4 },
                { "Matrix Transformation", IElementPropertyType.Double4x4 },
                { "Matrix Translation", IElementPropertyType.Double4x4 },
                { "Matrix Rotation", IElementPropertyType.Double4x4 },
                { "Matrix Scaling", IElementPropertyType.Double4x4 },
                { "Emissive", IElementPropertyType.Double3 },
                { "EmissiveFactor", IElementPropertyType.Double },
                { "Ambient", IElementPropertyType.Double3 },
                { "AmbientFactor", IElementPropertyType.Double },
                { "Diffuse", IElementPropertyType.Double3 },
                { "DiffuseFactor", IElementPropertyType.Double },
                { "NormalMap", IElementPropertyType.Double3 },
                { "Bump", IElementPropertyType.Double },
                { "Transparent", IElementPropertyType.Double3 },
                { "TransparencyFactor", IElementPropertyType.Double },
                { "Specular", IElementPropertyType.Double3 },
                { "SpecularFactor", IElementPropertyType.Double },
                { "Shininess", IElementPropertyType.Double },
                { "Reflection", IElementPropertyType.Double3 },
                { "ReflectionFactor", IElementPropertyType.Double },
                { "Displacement", IElementPropertyType.Double3 },
                { "VectorDisplacement", IElementPropertyType.Double3 },
                { "Unknown Factor", IElementPropertyType.Double },
                { "Unknown texture", IElementPropertyType.Double3 },
                { "Url", IElementPropertyType.String },
                { "XRefUrl", IElementPropertyType.String },
                { "LayerElementUndefined", IElementPropertyType.Undefined },
                { "LayerElementNormal", IElementPropertyType.Double4 },
                { "LayerElementBinormal", IElementPropertyType.Double4 },
                { "LayerElementTangent", IElementPropertyType.Double4 },
                { "LayerElementMaterial", IElementPropertyType.Reference },
                { "LayerElementTexture", IElementPropertyType.Reference },
                { "LayerElementPolygonGroup", IElementPropertyType.Int },
                { "LayerElementUV", IElementPropertyType.Double2 },
                { "LayerElementVertexColor", IElementPropertyType.Double4 },
                { "LayerElementSmoothing", IElementPropertyType.Int },
                { "LayerElementCrease", IElementPropertyType.Double },
                { "LayerElementHole", IElementPropertyType.Bool },
                { "LayerElementUserData", IElementPropertyType.Reference },
                { "LayerElementVisibility", IElementPropertyType.Bool },
                { "Intensity", IElementPropertyType.Double },
                { "Cone angle", IElementPropertyType.Double },
                { "Fog", IElementPropertyType.Double },
                { "Shape", IElementPropertyType.Double },
                { "FieldOfView", IElementPropertyType.Double },
                { "FieldOfViewX", IElementPropertyType.Double },
                { "FieldOfViewY", IElementPropertyType.Double },
                { "OpticalCenterX", IElementPropertyType.Double },
                { "OpticalCenterY", IElementPropertyType.Double },
                { "Roll", IElementPropertyType.Double },
                { "Camera Index", IElementPropertyType.Int },
                { "TimeWarp", IElementPropertyType.Double },
                { "Visibility", IElementPropertyType.Double },
                { "Visibility Inheritance", IElementPropertyType.Bool },
                { "Translation UV", IElementPropertyType.Double3 },
                { "Scaling UV", IElementPropertyType.Double3 },
                { "TextureRotation", IElementPropertyType.Double3 },
                { "HSB", IElementPropertyType.Double3 },
                { "Orientation", IElementPropertyType.Double3 },
                { "Look at", IElementPropertyType.Double3 },
                { "Occlusion", IElementPropertyType.Double },
                { "Weight", IElementPropertyType.Double },
                { "IK Reach Translation", IElementPropertyType.Double },
                { "IK Reach Rotation", IElementPropertyType.Double },
                { "Presets", IElementPropertyType.Enum },
                { "Statistics", IElementPropertyType.String },
                { "Units", IElementPropertyType.String },
                { "Warning", IElementPropertyType.String },
                { "Web", IElementPropertyType.String },
                { "TextLine", IElementPropertyType.String },
                { "Alias", IElementPropertyType.Enum }
            };

        private static readonly Dictionary<IElementPropertyType, AttributeCreator> ms_activators =
            new Dictionary<IElementPropertyType, AttributeCreator>
            {
                { IElementPropertyType.Undefined, AsAttributeAny },
                { IElementPropertyType.SByte, AsAttributeSByte },
                { IElementPropertyType.Byte, AsAttributeByte },
                { IElementPropertyType.Short, AsAttributeShort },
                { IElementPropertyType.UShort, AsAttributeUShort },
                { IElementPropertyType.UInt, AsAttributeUInt },
                { IElementPropertyType.Long, AsAttributeLong },
                { IElementPropertyType.ULong, AsAttributeULong },
                { IElementPropertyType.Half, AsAttributeHalf },
                { IElementPropertyType.Bool, AsAttributeBool },
                { IElementPropertyType.Int, AsAttributeInt },
                { IElementPropertyType.Float, AsAttributeFloat },
                { IElementPropertyType.Double, AsAttributeDouble },
                { IElementPropertyType.Double2, AsAttributeDouble2 },
                { IElementPropertyType.Double3, AsAttributeDouble3 },
                { IElementPropertyType.Double4, AsAttributeDouble4 },
                { IElementPropertyType.Double4x4, AsAttributeDouble4x4 },
                { IElementPropertyType.Enum, AsAttributeEnum },
                { IElementPropertyType.String, AsAttributeString },
                { IElementPropertyType.Time, AsAttributeTime },
                { IElementPropertyType.Reference, AsAttributeReference },
                { IElementPropertyType.Blob, AsAttributeBlob },
                { IElementPropertyType.Distance, AsAttributeDistance },
                { IElementPropertyType.DateTime, AsAttributeDateTime }
            };

        private static IElementPropertyFlags InternalParseFlags(IElement element)
        {
            var result = IElementPropertyFlags.Imported;
            var buffer = element.Attributes[3].GetElementValue().ToString();

            if (buffer.IndexOf('A') >= 0) result |= IElementPropertyFlags.Animatable;

            if (buffer.IndexOf('+') >= 0) result |= IElementPropertyFlags.Animated;

            if (buffer.IndexOf('U') >= 0) result |= IElementPropertyFlags.UserDefined;

            if (buffer.IndexOf('H') >= 0) result |= IElementPropertyFlags.Hidden;

            if (buffer.IndexOf('N') >= 0) result |= IElementPropertyFlags.NotSavable;

            var @lock = buffer.IndexOf('L');
            var muted = buffer.IndexOf('M');

            if (@lock >= 0)
            {
                var value = buffer[@lock + 1];

                if (value >= '1' && value <= '9')
                    result |= (IElementPropertyFlags)((value - '0') << 7);
                else if (value < 'a' || value > 'e')
                    result |= (IElementPropertyFlags)(0b00001111 << 7);
                else
                    result |= (IElementPropertyFlags)((value - 'W') << 7);
            }

            if (muted >= 0)
            {
                var value = buffer[muted + 1];

                if (value >= '1' && value <= '9')
                    result |= (IElementPropertyFlags)((value - '0') << 11);
                else if (value < 'a' || value > 'e')
                    result |= (IElementPropertyFlags)(0b00001111 << 11);
                else
                    result |= (IElementPropertyFlags)((value - 'W') << 11);
            }

            return result;
        }

        private static bool IsLimitExtendibleFlag(IElementPropertyFlags flags)
        {
            return (flags & IElementPropertyFlags.Animatable) != 0 && (flags & IElementPropertyFlags.UserDefined) != 0;
        }

        private static FBXProperty<T> InternalCreateProperty<T>(IElement element, T value)
        {
            return new FBXProperty<T>
            (
                element.Attributes[1].GetElementValue().ToString(),
                element.Attributes[2].GetElementValue().ToString(),
                element.Attributes[0].GetElementValue().ToString(),
                InternalParseFlags(element),
                value
            );
        }

        private static IElementAttribute[] InternalMakeAttribArray(IElementProperty property, int numAttribs)
        {
            var attributes = new IElementAttribute[numAttribs];
            var useFlag = string.Empty;

            if ((property.Flags & IElementPropertyFlags.Animatable) != 0)
            {
                useFlag += "A";

                if ((property.Flags & IElementPropertyFlags.Animated) != 0) useFlag += "+";
            }

            if ((property.Flags & IElementPropertyFlags.UserDefined) != 0) useFlag += "U";

            if ((property.Flags & IElementPropertyFlags.Hidden) != 0) useFlag += "H";

            if ((property.Flags & IElementPropertyFlags.NotSavable) != 0) useFlag += "N";

            attributes[0] = ElementaryFactory.GetElementAttribute(property.Name);
            attributes[1] = ElementaryFactory.GetElementAttribute(property.Primary);
            attributes[2] = ElementaryFactory.GetElementAttribute(property.Secondary);
            attributes[3] = ElementaryFactory.GetElementAttribute(useFlag);

            return attributes;
        }

        private static IElementProperty AsAttributeAny(IElement element)
        {
            if (element.Attributes.Length < 4) return null;

            if (element.Attributes.Length == 4) return InternalCreateProperty<object>(element, null);

            var array = new object[element.Attributes.Length - 4];

            for (int i = 0, k = 4; i < array.Length; ++i, ++k) array[i] = element.Attributes[k];

            return InternalCreateProperty(element, array);
        }

        private static IElementProperty AsAttributeSByte(IElement element)
        {
            if (element.Attributes.Length < 5) return null;

            var property = InternalCreateProperty(element, Convert.ToSByte(element.Attributes[4].GetElementValue()));

            if ((property.Flags & IElementPropertyFlags.UserDefined) != 0)
            {
                if (element.Attributes.Length >= 6)
                    property.SetMinValue(Convert.ToSByte(element.Attributes[5].GetElementValue()));

                if (element.Attributes.Length >= 7)
                    property.SetMaxValue(Convert.ToSByte(element.Attributes[6].GetElementValue()));
            }

            return property;
        }

        private static IElementProperty AsAttributeByte(IElement element)
        {
            if (element.Attributes.Length < 5) return null;

            var property = InternalCreateProperty(element, Convert.ToByte(element.Attributes[4].GetElementValue()));

            if ((property.Flags & IElementPropertyFlags.UserDefined) != 0)
            {
                if (element.Attributes.Length >= 6)
                    property.SetMinValue(Convert.ToByte(element.Attributes[5].GetElementValue()));

                if (element.Attributes.Length >= 7)
                    property.SetMaxValue(Convert.ToByte(element.Attributes[6].GetElementValue()));
            }

            return property;
        }

        private static IElementProperty AsAttributeShort(IElement element)
        {
            if (element.Attributes.Length < 5) return null;

            var property = InternalCreateProperty(element, Convert.ToInt16(element.Attributes[4].GetElementValue()));

            if ((property.Flags & IElementPropertyFlags.UserDefined) != 0)
            {
                if (element.Attributes.Length >= 6)
                    property.SetMinValue(Convert.ToInt16(element.Attributes[5].GetElementValue()));

                if (element.Attributes.Length >= 7)
                    property.SetMaxValue(Convert.ToInt16(element.Attributes[6].GetElementValue()));
            }

            return property;
        }

        private static IElementProperty AsAttributeUShort(IElement element)
        {
            if (element.Attributes.Length < 5) return null;

            var property = InternalCreateProperty(element, Convert.ToUInt16(element.Attributes[4].GetElementValue()));

            if ((property.Flags & IElementPropertyFlags.UserDefined) != 0)
            {
                if (element.Attributes.Length >= 6)
                    property.SetMinValue(Convert.ToUInt16(element.Attributes[5].GetElementValue()));

                if (element.Attributes.Length >= 7)
                    property.SetMaxValue(Convert.ToUInt16(element.Attributes[6].GetElementValue()));
            }

            return property;
        }

        private static IElementProperty AsAttributeUInt(IElement element)
        {
            if (element.Attributes.Length < 5) return null;

            var property = InternalCreateProperty(element, Convert.ToUInt32(element.Attributes[4].GetElementValue()));

            if ((property.Flags & IElementPropertyFlags.UserDefined) != 0)
            {
                if (element.Attributes.Length >= 6)
                    property.SetMinValue(Convert.ToUInt32(element.Attributes[5].GetElementValue()));

                if (element.Attributes.Length >= 7)
                    property.SetMaxValue(Convert.ToUInt32(element.Attributes[6].GetElementValue()));
            }

            return property;
        }

        private static IElementProperty AsAttributeLong(IElement element)
        {
            if (element.Attributes.Length < 5) return null;

            var property = InternalCreateProperty(element, Convert.ToInt64(element.Attributes[4].GetElementValue()));

            if ((property.Flags & IElementPropertyFlags.UserDefined) != 0)
            {
                if (element.Attributes.Length >= 6)
                    property.SetMinValue(Convert.ToInt64(element.Attributes[5].GetElementValue()));

                if (element.Attributes.Length >= 7)
                    property.SetMaxValue(Convert.ToInt64(element.Attributes[6].GetElementValue()));
            }

            return property;
        }

        private static IElementProperty AsAttributeULong(IElement element)
        {
            if (element.Attributes.Length < 5) return null;

            var property = InternalCreateProperty(element, Convert.ToUInt64(element.Attributes[4].GetElementValue()));

            if ((property.Flags & IElementPropertyFlags.UserDefined) != 0)
            {
                if (element.Attributes.Length >= 6)
                    property.SetMinValue(Convert.ToUInt64(element.Attributes[5].GetElementValue()));

                if (element.Attributes.Length >= 7)
                    property.SetMaxValue(Convert.ToUInt64(element.Attributes[6].GetElementValue()));
            }

            return property;
        }

        private static IElementProperty AsAttributeHalf(IElement element)
        {
            if (element.Attributes.Length < 5) return null;

            var property = InternalCreateProperty(element,
                new Half(Convert.ToSingle(element.Attributes[4].GetElementValue())));

            if ((property.Flags & IElementPropertyFlags.UserDefined) != 0)
            {
                if (element.Attributes.Length >= 6)
                    property.SetMinValue(new Half((float)Convert.ToDouble(element.Attributes[5].GetElementValue())));

                if (element.Attributes.Length >= 7)
                    property.SetMaxValue(new Half((float)Convert.ToDouble(element.Attributes[6].GetElementValue())));
            }

            return property;
        }

        private static IElementProperty AsAttributeBool(IElement element)
        {
            if (element.Attributes.Length < 5) return null;

            var property = InternalCreateProperty(element, Convert.ToBoolean(element.Attributes[4].GetElementValue()));

            if ((property.Flags & IElementPropertyFlags.UserDefined) != 0)
            {
                if (element.Attributes.Length >= 6)
                    property.SetMinValue(Convert.ToDouble(element.Attributes[5].GetElementValue()) != 0.0);

                if (element.Attributes.Length >= 7)
                    property.SetMaxValue(Convert.ToDouble(element.Attributes[6].GetElementValue()) != 0.0);
            }

            return property;
        }

        private static IElementProperty AsAttributeInt(IElement element)
        {
            if (element.Attributes.Length < 5) return null;

            var property = InternalCreateProperty(element, Convert.ToInt32(element.Attributes[4].GetElementValue()));

            if ((property.Flags & IElementPropertyFlags.UserDefined) != 0)
            {
                if (element.Attributes.Length >= 6)
                    property.SetMinValue(Convert.ToInt32(element.Attributes[5].GetElementValue()));

                if (element.Attributes.Length >= 7)
                    property.SetMaxValue(Convert.ToInt32(element.Attributes[6].GetElementValue()));
            }

            return property;
        }

        private static IElementProperty AsAttributeFloat(IElement element)
        {
            if (element.Attributes.Length < 5) return null;

            var property = InternalCreateProperty(element, Convert.ToSingle(element.Attributes[4].GetElementValue()));

            if ((property.Flags & IElementPropertyFlags.UserDefined) != 0)
            {
                if (element.Attributes.Length >= 6)
                    property.SetMinValue((float)Convert.ToDouble(element.Attributes[5].GetElementValue()));

                if (element.Attributes.Length >= 7)
                    property.SetMaxValue((float)Convert.ToDouble(element.Attributes[6].GetElementValue()));
            }

            return property;
        }

        private static IElementProperty AsAttributeDouble(IElement element)
        {
            if (element.Attributes.Length < 5) return null;

            var property = InternalCreateProperty(element, Convert.ToDouble(element.Attributes[4].GetElementValue()));

            if ((property.Flags & IElementPropertyFlags.UserDefined) != 0)
            {
                if (element.Attributes.Length >= 6)
                    property.SetMinValue(Convert.ToDouble(element.Attributes[5].GetElementValue()));

                if (element.Attributes.Length >= 7)
                    property.SetMaxValue(Convert.ToDouble(element.Attributes[6].GetElementValue()));
            }

            return property;
        }

        private static IElementProperty AsAttributeDouble2(IElement element)
        {
            if (element.Attributes.Length < 6) return null;

            return InternalCreateProperty(element, new Vector2
            (
                Convert.ToDouble(element.Attributes[4].GetElementValue()),
                Convert.ToDouble(element.Attributes[5].GetElementValue())
            ));
        }

        private static IElementProperty AsAttributeDouble3(IElement element)
        {
            if (element.Attributes.Length < 7) return null;

            return InternalCreateProperty(element, new Vector3
            (
                Convert.ToDouble(element.Attributes[4].GetElementValue()),
                Convert.ToDouble(element.Attributes[5].GetElementValue()),
                Convert.ToDouble(element.Attributes[6].GetElementValue())
            ));
        }

        private static IElementProperty AsAttributeDouble4(IElement element)
        {
            if (element.Attributes.Length < 8) return null;

            return InternalCreateProperty(element, new Vector4
            (
                Convert.ToDouble(element.Attributes[4].GetElementValue()),
                Convert.ToDouble(element.Attributes[5].GetElementValue()),
                Convert.ToDouble(element.Attributes[6].GetElementValue()),
                Convert.ToDouble(element.Attributes[7].GetElementValue())
            ));
        }

        private static IElementProperty AsAttributeDouble4x4(IElement element)
        {
            if (element.Attributes.Length < 20) return null;

            return InternalCreateProperty(element, new Matrix4x4
            (
                Convert.ToDouble(element.Attributes[4].GetElementValue()),
                Convert.ToDouble(element.Attributes[5].GetElementValue()),
                Convert.ToDouble(element.Attributes[6].GetElementValue()),
                Convert.ToDouble(element.Attributes[7].GetElementValue()),
                Convert.ToDouble(element.Attributes[8].GetElementValue()),
                Convert.ToDouble(element.Attributes[9].GetElementValue()),
                Convert.ToDouble(element.Attributes[10].GetElementValue()),
                Convert.ToDouble(element.Attributes[11].GetElementValue()),
                Convert.ToDouble(element.Attributes[12].GetElementValue()),
                Convert.ToDouble(element.Attributes[13].GetElementValue()),
                Convert.ToDouble(element.Attributes[14].GetElementValue()),
                Convert.ToDouble(element.Attributes[15].GetElementValue()),
                Convert.ToDouble(element.Attributes[16].GetElementValue()),
                Convert.ToDouble(element.Attributes[17].GetElementValue()),
                Convert.ToDouble(element.Attributes[18].GetElementValue()),
                Convert.ToDouble(element.Attributes[19].GetElementValue())
            ));
        }

        private static IElementProperty AsAttributeEnum(IElement element)
        {
            if (element.Attributes.Length < 5) return null;

            var enumprop = new Enumeration(Convert.ToInt32(element.Attributes[4].GetElementValue()));
            var property = InternalCreateProperty(element, enumprop);

            if ((property.Flags & IElementPropertyFlags.UserDefined) != 0)
                if (element.Attributes.Length >= 6)
                    enumprop.Flags = element.Attributes[5].GetElementValue().ToString().Split('~');

            return property;
        }

        private static IElementProperty AsAttributeString(IElement element)
        {
            if (element.Attributes.Length < 5) return null;

            return InternalCreateProperty(element, element.Attributes[4].GetElementValue().ToString());
        }

        private static IElementProperty AsAttributeTime(IElement element)
        {
            if (element.Attributes.Length < 5) return null;

            if (element.Attributes[4].Type == IElementAttributeType.Int64)
                return InternalCreateProperty(element,
                    new TimeBase(Convert.ToInt64(element.Attributes[4].GetElementValue())));
            return InternalCreateProperty(element,
                new TimeBase(Convert.ToDouble(element.Attributes[4].GetElementValue())));
        }

        private static IElementProperty AsAttributeReference(IElement element)
        {
            if (element.Attributes.Length < 4) return null;

            return InternalCreateProperty(element, new Reference());
        }

        private static IElementProperty AsAttributeBlob(IElement element)
        {
            if (element.Attributes.Length < 4) return null;

            var array = new object[element.Attributes.Length - 4];

            for (int i = 0, k = 4; i < array.Length; ++i, ++k) array[i] = element.Attributes[k];

            return InternalCreateProperty(element, new BinaryBlob(array));
        }

        private static IElementProperty AsAttributeDistance(IElement element)
        {
            if (element.Attributes.Length < 6) return null;

            return InternalCreateProperty(element, new Distance
            (
                Convert.ToSingle(element.Attributes[4].GetElementValue()),
                element.Attributes[5].GetElementValue().ToString()
            ));
        }

        private static IElementProperty AsAttributeDateTime(IElement element)
        {
            if (element.Attributes.Length < 5) return null;

            var strinc = element.Attributes[4].GetElementValue().ToString();
            var splits = strinc.Split(new[] { ' ', '/', ':', '.' }, StringSplitOptions.RemoveEmptyEntries);

            return InternalCreateProperty(element, new DateTime
            (
                splits.Length > 2 ? int.Parse(splits[2]) : 0,
                splits.Length > 1 ? int.Parse(splits[1]) : 0,
                splits.Length > 0 ? int.Parse(splits[0]) : 0,
                splits.Length > 3 ? int.Parse(splits[3]) : 0,
                splits.Length > 4 ? int.Parse(splits[4]) : 0,
                splits.Length > 5 ? int.Parse(splits[5]) : 0,
                splits.Length > 6 ? int.Parse(splits[6]) : 0
            ));
        }

        private static IElement AsPrimitiveAny(IElementProperty property)
        {
            var value = property.GetPropertyValue();

            if (value is null) return new Element("P", null, InternalMakeAttribArray(property, 4));

            if (value is object[] array)
            {
                var attrib = InternalMakeAttribArray(property, 4 + array.Length);

                for (var i = 0; i < array.Length; ++i) attrib[4 + i] = ElementaryFactory.GetElementAttribute(array[i]);

                return new Element("P", null, attrib);
            }

            var result = InternalMakeAttribArray(property, 5);

            result[4] = ElementaryFactory.GetElementAttribute(value);

            return new Element("P", null, result);
        }

        private static IElement AsPrimitiveSByte(IElementProperty property)
        {
            var isUser = IsLimitExtendibleFlag(property.Flags);
            var attrib = InternalMakeAttribArray(property, isUser ? 7 : 5);

            attrib[4] = ElementaryFactory.GetElementAttribute((sbyte)property.GetPropertyValue());

            if (isUser)
            {
                attrib[5] = ElementaryFactory.GetElementAttribute((sbyte)property.GetPropertyMin());
                attrib[6] = ElementaryFactory.GetElementAttribute((sbyte)property.GetPropertyMax());
            }

            return new Element("P", null, attrib);
        }

        private static IElement AsPrimitiveByte(IElementProperty property)
        {
            var isUser = IsLimitExtendibleFlag(property.Flags);
            var attrib = InternalMakeAttribArray(property, isUser ? 7 : 5);

            attrib[4] = ElementaryFactory.GetElementAttribute((byte)property.GetPropertyValue());

            if (isUser)
            {
                attrib[5] = ElementaryFactory.GetElementAttribute((byte)property.GetPropertyMin());
                attrib[6] = ElementaryFactory.GetElementAttribute((byte)property.GetPropertyMax());
            }

            return new Element("P", null, attrib);
        }

        private static IElement AsPrimitiveShort(IElementProperty property)
        {
            var isUser = IsLimitExtendibleFlag(property.Flags);
            var attrib = InternalMakeAttribArray(property, isUser ? 7 : 5);

            attrib[4] = ElementaryFactory.GetElementAttribute((short)property.GetPropertyValue());

            if (isUser)
            {
                attrib[5] = ElementaryFactory.GetElementAttribute((short)property.GetPropertyMin());
                attrib[6] = ElementaryFactory.GetElementAttribute((short)property.GetPropertyMax());
            }

            return new Element("P", null, attrib);
        }

        private static IElement AsPrimitiveUShort(IElementProperty property)
        {
            var isUser = IsLimitExtendibleFlag(property.Flags);
            var attrib = InternalMakeAttribArray(property, isUser ? 7 : 5);

            attrib[4] = ElementaryFactory.GetElementAttribute((ushort)property.GetPropertyValue());

            if (isUser)
            {
                attrib[5] = ElementaryFactory.GetElementAttribute((ushort)property.GetPropertyMin());
                attrib[6] = ElementaryFactory.GetElementAttribute((ushort)property.GetPropertyMax());
            }

            return new Element("P", null, attrib);
        }

        private static IElement AsPrimitiveUInt(IElementProperty property)
        {
            var isUser = IsLimitExtendibleFlag(property.Flags);
            var attrib = InternalMakeAttribArray(property, isUser ? 7 : 5);

            attrib[4] = ElementaryFactory.GetElementAttribute((uint)property.GetPropertyValue());

            if (isUser)
            {
                attrib[5] = ElementaryFactory.GetElementAttribute((uint)property.GetPropertyMin());
                attrib[6] = ElementaryFactory.GetElementAttribute((uint)property.GetPropertyMax());
            }

            return new Element("P", null, attrib);
        }

        private static IElement AsPrimitiveLong(IElementProperty property)
        {
            var isUser = IsLimitExtendibleFlag(property.Flags);
            var attrib = InternalMakeAttribArray(property, isUser ? 7 : 5);

            attrib[4] = ElementaryFactory.GetElementAttribute((long)property.GetPropertyValue());

            if (isUser)
            {
                attrib[5] = ElementaryFactory.GetElementAttribute((long)property.GetPropertyMin());
                attrib[6] = ElementaryFactory.GetElementAttribute((long)property.GetPropertyMax());
            }

            return new Element("P", null, attrib);
        }

        private static IElement AsPrimitiveULong(IElementProperty property)
        {
            var isUser = IsLimitExtendibleFlag(property.Flags);
            var attrib = InternalMakeAttribArray(property, isUser ? 7 : 5);

            attrib[4] = ElementaryFactory.GetElementAttribute((ulong)property.GetPropertyValue());

            if (isUser)
            {
                attrib[5] = ElementaryFactory.GetElementAttribute((ulong)property.GetPropertyMin());
                attrib[6] = ElementaryFactory.GetElementAttribute((ulong)property.GetPropertyMax());
            }

            return new Element("P", null, attrib);
        }

        private static IElement AsPrimitiveHalf(IElementProperty property)
        {
            var isUser = IsLimitExtendibleFlag(property.Flags);
            var attrib = InternalMakeAttribArray(property, isUser ? 7 : 5);

            attrib[4] = ElementaryFactory.GetElementAttribute(((Half)property.GetPropertyValue()).ToSingle());

            if (isUser)
            {
                attrib[5] = ElementaryFactory.GetElementAttribute(((Half)property.GetPropertyMin()).ToSingle());
                attrib[6] = ElementaryFactory.GetElementAttribute(((Half)property.GetPropertyMax()).ToSingle());
            }

            return new Element("P", null, attrib);
        }

        private static IElement AsPrimitiveBool(IElementProperty property)
        {
            var isUser = IsLimitExtendibleFlag(property.Flags);
            var attrib = InternalMakeAttribArray(property, isUser ? 7 : 5);

            attrib[4] = ElementaryFactory.GetElementAttribute((bool)property.GetPropertyValue());

            if (isUser)
            {
                attrib[5] = ElementaryFactory.GetElementAttribute((bool)property.GetPropertyMin());
                attrib[6] = ElementaryFactory.GetElementAttribute((bool)property.GetPropertyMax());
            }

            return new Element("P", null, attrib);
        }

        private static IElement AsPrimitiveInt(IElementProperty property)
        {
            var isUser = IsLimitExtendibleFlag(property.Flags);
            var attrib = InternalMakeAttribArray(property, isUser ? 7 : 5);

            attrib[4] = ElementaryFactory.GetElementAttribute((int)property.GetPropertyValue());

            if (isUser)
            {
                attrib[5] = ElementaryFactory.GetElementAttribute((int)property.GetPropertyMin());
                attrib[6] = ElementaryFactory.GetElementAttribute((int)property.GetPropertyMax());
            }

            return new Element("P", null, attrib);
        }

        private static IElement AsPrimitiveFloat(IElementProperty property)
        {
            var isUser = IsLimitExtendibleFlag(property.Flags);
            var attrib = InternalMakeAttribArray(property, isUser ? 7 : 5);

            attrib[4] = ElementaryFactory.GetElementAttribute((float)property.GetPropertyValue());

            if (isUser)
            {
                attrib[5] = ElementaryFactory.GetElementAttribute((float)property.GetPropertyMin());
                attrib[6] = ElementaryFactory.GetElementAttribute((float)property.GetPropertyMax());
            }

            return new Element("P", null, attrib);
        }

        private static IElement AsPrimitiveDouble(IElementProperty property)
        {
            var isUser = IsLimitExtendibleFlag(property.Flags);
            var attrib = InternalMakeAttribArray(property, isUser ? 7 : 5);

            attrib[4] = ElementaryFactory.GetElementAttribute((double)property.GetPropertyValue());

            if (isUser)
            {
                attrib[5] = ElementaryFactory.GetElementAttribute((double)property.GetPropertyMin());
                attrib[6] = ElementaryFactory.GetElementAttribute((double)property.GetPropertyMax());
            }

            return new Element("P", null, attrib);
        }

        private static IElement AsPrimitiveDouble2(IElementProperty property)
        {
            var attrib = InternalMakeAttribArray(property, 6);
            var vector = (Vector2)property.GetPropertyValue();

            attrib[4] = ElementaryFactory.GetElementAttribute(vector.X);
            attrib[5] = ElementaryFactory.GetElementAttribute(vector.Y);

            return new Element("P", null, attrib);
        }

        private static IElement AsPrimitiveDouble3(IElementProperty property)
        {
            var attrib = InternalMakeAttribArray(property, 7);
            var vector = (Vector3)property.GetPropertyValue();

            attrib[4] = ElementaryFactory.GetElementAttribute(vector.X);
            attrib[5] = ElementaryFactory.GetElementAttribute(vector.Y);
            attrib[6] = ElementaryFactory.GetElementAttribute(vector.Z);

            return new Element("P", null, attrib);
        }

        private static IElement AsPrimitiveDouble4(IElementProperty property)
        {
            var attrib = InternalMakeAttribArray(property, 8);
            var vector = (Vector4)property.GetPropertyValue();

            attrib[4] = ElementaryFactory.GetElementAttribute(vector.X);
            attrib[5] = ElementaryFactory.GetElementAttribute(vector.Y);
            attrib[6] = ElementaryFactory.GetElementAttribute(vector.Z);
            attrib[7] = ElementaryFactory.GetElementAttribute(vector.W);

            return new Element("P", null, attrib);
        }

        private static IElement AsPrimitiveDouble4x4(IElementProperty property)
        {
            var attrib = InternalMakeAttribArray(property, 20);
            var matrix = (Matrix4x4)property.GetPropertyValue();

            attrib[4] = ElementaryFactory.GetElementAttribute(matrix.M11);
            attrib[5] = ElementaryFactory.GetElementAttribute(matrix.M12);
            attrib[6] = ElementaryFactory.GetElementAttribute(matrix.M13);
            attrib[7] = ElementaryFactory.GetElementAttribute(matrix.M14);
            attrib[8] = ElementaryFactory.GetElementAttribute(matrix.M21);
            attrib[9] = ElementaryFactory.GetElementAttribute(matrix.M22);
            attrib[10] = ElementaryFactory.GetElementAttribute(matrix.M23);
            attrib[11] = ElementaryFactory.GetElementAttribute(matrix.M24);
            attrib[12] = ElementaryFactory.GetElementAttribute(matrix.M31);
            attrib[13] = ElementaryFactory.GetElementAttribute(matrix.M32);
            attrib[14] = ElementaryFactory.GetElementAttribute(matrix.M33);
            attrib[15] = ElementaryFactory.GetElementAttribute(matrix.M34);
            attrib[16] = ElementaryFactory.GetElementAttribute(matrix.M41);
            attrib[17] = ElementaryFactory.GetElementAttribute(matrix.M42);
            attrib[18] = ElementaryFactory.GetElementAttribute(matrix.M43);
            attrib[19] = ElementaryFactory.GetElementAttribute(matrix.M44);

            return new Element("P", null, attrib);
        }

        private static IElement AsPrimitiveEnum(IElementProperty property)
        {
            var isUser = (property.Flags & IElementPropertyFlags.UserDefined) != 0;
            var attrib = InternalMakeAttribArray(property, isUser ? 6 : 5);
            var enumpr = property.GetPropertyValue() as Enumeration;

            attrib[4] = ElementaryFactory.GetElementAttribute(enumpr.Value);

            if (isUser) attrib[5] = ElementaryFactory.GetElementAttribute(enumpr.Flags.Join("~"));

            return new Element("P", null, attrib);
        }

        private static IElement AsPrimitiveString(IElementProperty property)
        {
            var attrib = InternalMakeAttribArray(property, 5);

            attrib[4] = ElementaryFactory.GetElementAttribute(property.GetPropertyValue().ToString());

            return new Element("P", null, attrib);
        }

        private static IElement AsPrimitiveTime(IElementProperty property)
        {
            var attrib = InternalMakeAttribArray(property, 5);

            attrib[4] = ElementaryFactory.GetElementAttribute(((TimeBase)property.GetPropertyValue()).ToLong());

            return new Element("P", null, attrib);
        }

        private static IElement AsPrimitiveReference(IElementProperty property)
        {
            return new Element("P", null, InternalMakeAttribArray(property, 4));
        }

        private static IElement AsPrimitiveBlob(IElementProperty property)
        {
            var blober = property.GetPropertyValue() as BinaryBlob;
            var attrib = InternalMakeAttribArray(property, 4 + blober.Datas.Length);

            for (var i = 0; i < blober.Datas.Length; ++i)
                attrib[4 + i] = ElementaryFactory.GetElementAttribute(blober.Datas[i]);

            return new Element("P", null, attrib);
        }

        private static IElement AsPrimitiveDistance(IElementProperty property)
        {
            var attrib = InternalMakeAttribArray(property, 6);
            var vector = (Distance)property.GetPropertyValue();

            attrib[4] = ElementaryFactory.GetElementAttribute(vector.Value);
            attrib[5] = ElementaryFactory.GetElementAttribute(vector.Unit);

            return new Element("P", null, attrib);
        }

        private static IElement AsPrimitiveDateTime(IElementProperty property)
        {
            var attrib = InternalMakeAttribArray(property, 5);

            attrib[4] = ElementaryFactory.GetElementAttribute(
                ((DateTime)property.GetPropertyValue()).ToString("dd/MM/yyyy HH:mm:ss.fff"));

            return new Element("P", null, attrib);
        }

        public static IElementProperty AsElementProperty(IElement element)
        {
            if (element is null || element.Attributes.Length < 4) return null;

            if (ms_typeMapper.TryGetValue(element.Attributes[2].GetElementValue().ToString(), out var type))
                return ms_activators[type].Invoke(element);

            if (ms_typeMapper.TryGetValue(element.Attributes[1].GetElementValue().ToString(), out type))
                return ms_activators[type].Invoke(element);
            return ms_activators[IElementPropertyType.Undefined].Invoke(element);
        }

        public static IElement AsElement(IElementProperty property)
        {
            switch (property.Type)
            {
                case IElementPropertyType.Undefined: return AsPrimitiveAny(property);
                case IElementPropertyType.SByte: return AsPrimitiveSByte(property);
                case IElementPropertyType.Byte: return AsPrimitiveByte(property);
                case IElementPropertyType.Short: return AsPrimitiveShort(property);
                case IElementPropertyType.UShort: return AsPrimitiveUShort(property);
                case IElementPropertyType.UInt: return AsPrimitiveUInt(property);
                case IElementPropertyType.Long: return AsPrimitiveLong(property);
                case IElementPropertyType.ULong: return AsPrimitiveULong(property);
                case IElementPropertyType.Half: return AsPrimitiveHalf(property);
                case IElementPropertyType.Bool: return AsPrimitiveBool(property);
                case IElementPropertyType.Int: return AsPrimitiveInt(property);
                case IElementPropertyType.Float: return AsPrimitiveFloat(property);
                case IElementPropertyType.Double: return AsPrimitiveDouble(property);
                case IElementPropertyType.Double2: return AsPrimitiveDouble2(property);
                case IElementPropertyType.Double3: return AsPrimitiveDouble3(property);
                case IElementPropertyType.Double4: return AsPrimitiveDouble4(property);
                case IElementPropertyType.Double4x4: return AsPrimitiveDouble4x4(property);
                case IElementPropertyType.Enum: return AsPrimitiveEnum(property);
                case IElementPropertyType.String: return AsPrimitiveString(property);
                case IElementPropertyType.Time: return AsPrimitiveTime(property);
                case IElementPropertyType.Reference: return AsPrimitiveReference(property);
                case IElementPropertyType.Blob: return AsPrimitiveBlob(property);
                case IElementPropertyType.Distance: return AsPrimitiveDistance(property);
                case IElementPropertyType.DateTime: return AsPrimitiveDateTime(property);
                default: return null;
            }
        }
    }
}