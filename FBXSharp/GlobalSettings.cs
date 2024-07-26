using System;
using FBXSharp.Core;
using FBXSharp.ValueTypes;

namespace FBXSharp
{
    public class GlobalSettings : FBXObject
    {
        public static readonly FBXObjectType FType = FBXObjectType.GlobalSettings;

        public static readonly FBXClassType FClass = FBXClassType.GlobalSettings;

        public override FBXObjectType Type => FType;

        public override FBXClassType Class => FClass;

        public int? UpAxis
        {
            get => InternalGetPrimitive<int>(nameof(UpAxis), IElementPropertyType.Int);
            set => InternalSetPrimitive(nameof(UpAxis), IElementPropertyType.Int, value, "int", "Integer");
        }

        public int? UpAxisSign
        {
            get => InternalGetPrimitive<int>(nameof(UpAxisSign), IElementPropertyType.Int);
            set => InternalSetPrimitive(nameof(UpAxisSign), IElementPropertyType.Int, value, "int", "Integer");
        }

        public int? FrontAxis
        {
            get => InternalGetPrimitive<int>(nameof(FrontAxis), IElementPropertyType.Int);
            set => InternalSetPrimitive(nameof(FrontAxis), IElementPropertyType.Int, value, "int", "Integer");
        }

        public int? FrontAxisSign
        {
            get => InternalGetPrimitive<int>(nameof(FrontAxisSign), IElementPropertyType.Int);
            set => InternalSetPrimitive(nameof(FrontAxisSign), IElementPropertyType.Int, value, "int", "Integer");
        }

        public int? CoordAxis
        {
            get => InternalGetPrimitive<int>(nameof(CoordAxis), IElementPropertyType.Int);
            set => InternalSetPrimitive(nameof(CoordAxis), IElementPropertyType.Int, value, "int", "Integer");
        }

        public int? CoordAxisSign
        {
            get => InternalGetPrimitive<int>(nameof(CoordAxisSign), IElementPropertyType.Int);
            set => InternalSetPrimitive(nameof(CoordAxisSign), IElementPropertyType.Int, value, "int", "Integer");
        }

        public int? OriginalUpAxis
        {
            get => InternalGetPrimitive<int>(nameof(OriginalUpAxis), IElementPropertyType.Int);
            set => InternalSetPrimitive(nameof(OriginalUpAxis), IElementPropertyType.Int, value, "int", "Integer");
        }

        public int? OriginalUpAxisSign
        {
            get => InternalGetPrimitive<int>(nameof(OriginalUpAxisSign), IElementPropertyType.Int);
            set => InternalSetPrimitive(nameof(OriginalUpAxisSign), IElementPropertyType.Int, value, "int", "Integer");
        }

        public double? UnitScaleFactor
        {
            get => InternalGetPrimitive<double>(nameof(UnitScaleFactor), IElementPropertyType.Double);
            set => InternalSetPrimitive(nameof(UnitScaleFactor), IElementPropertyType.Double, value, "double",
                "Number");
        }

        public double? OriginalUnitScaleFactor
        {
            get => InternalGetPrimitive<double>(nameof(OriginalUnitScaleFactor), IElementPropertyType.Double);
            set => InternalSetPrimitive(nameof(OriginalUnitScaleFactor), IElementPropertyType.Double, value, "double",
                "Number");
        }

        public TimeBase? TimeSpanStart
        {
            get => InternalGetPrimitive<TimeBase>(nameof(TimeSpanStart), IElementPropertyType.Time);
            set => InternalSetPrimitive(nameof(TimeSpanStart), IElementPropertyType.Time, value, "KTime", "Time");
        }

        public TimeBase? TimeSpanStop
        {
            get => InternalGetPrimitive<TimeBase>(nameof(TimeSpanStop), IElementPropertyType.Time);
            set => InternalSetPrimitive(nameof(TimeSpanStop), IElementPropertyType.Time, value, "KTime", "Time");
        }

        public double? CustomFrameRate
        {
            get => InternalGetPrimitive<double>(nameof(CustomFrameRate), IElementPropertyType.Double);
            set => InternalSetPrimitive(nameof(CustomFrameRate), IElementPropertyType.Double, value, "double",
                "Number");
        }

        public Enumeration TimeMode
        {
            get => InternalGetEnumeration(nameof(TimeMode));
            set => InternalSetEnumeration(nameof(TimeMode), value, "enum", string.Empty);
        }

        internal GlobalSettings(IElement element, IScene scene) : base(element, scene)
        {
            Name = nameof(GlobalSettings);
        }

        internal void InternalFillWithElement(IElement element)
        {
            FromElement(element);
        }

        public override IElement AsElement(bool binary)
        {
            throw new NotSupportedException("Global Settings cannot be serialized");
        }
    }
}