using System;

namespace FBXSharp.Core
{
    [Flags]
    public enum IElementPropertyFlags
    {
        None = 0,
        Static = 1 << 0,
        Animatable = 1 << 1,
        Animated = 1 << 2,
        Imported = 1 << 3,
        UserDefined = 1 << 4,
        Hidden = 1 << 5,
        NotSavable = 1 << 6,

        LockedMember0 = 1 << 7,
        LockedMember1 = 1 << 8,
        LockedMember2 = 1 << 9,
        LockedMember3 = 1 << 10,
        LockedAll = LockedMember0 | LockedMember1 | LockedMember2 | LockedMember3,
        MutedMember0 = 1 << 11,
        MutedMember1 = 1 << 12,
        MutedMember2 = 1 << 13,
        MutedMember3 = 1 << 14,
        MutedAll = MutedMember0 | MutedMember1 | MutedMember2 | MutedMember3,

        UIDisabled = 1 << 15,
        UIGroup = 1 << 16,
        UIBoolGroup = 1 << 17,
        UIExpanded = 1 << 18,
        UINoCaption = 1 << 19,
        UIPanel = 1 << 20,
        UILeftLabel = 1 << 21,
        UIHidden = 1 << 22,

        CtrlFlags =
            Static | Animatable | Animated | Imported | UserDefined | Hidden | NotSavable | LockedAll | MutedAll,
        UIFlags = UIDisabled | UIGroup | UIBoolGroup | UIExpanded | UINoCaption | UIPanel | UILeftLabel | UIHidden,
        AllFlags = CtrlFlags | UIFlags,

        FlagCount = 23
    }

    public enum IElementPropertyType
    {
        Undefined,
        SByte,
        Byte,
        Short,
        UShort,
        UInt,
        Long,
        ULong,
        Half,
        Bool,
        Int,
        Float,
        Double,
        Double2,
        Double3,
        Double4,
        Double4x4,
        Enum,
        String,
        Time,
        Reference,
        Blob,
        Distance,
        DateTime,
        TypeCount
    }

    public interface IElementProperty
    {
        string Name { get; set; }

        IElementPropertyFlags Flags { get; set; }

        IElementPropertyType Type { get; }

        string Primary { get; }

        string Secondary { get; }

        bool SupportsMinMax { get; }

        Type GetPropertyType();

        object GetPropertyValue();
        object GetPropertyMin();
        object GetPropertyMax();

        void SetPropertyValue(object value);
        void SetPropertyMin(object value);
        void SetPropertyMax(object value);
    }

    public interface IGenericProperty<T> : IElementProperty
    {
        T Value { get; set; }

        bool GetMinValue(out T min);
        bool GetMaxValue(out T max);

        void SetMinValue(in T min);
        void SetMaxValue(in T max);
    }
}