using System;
using FBXSharp.Core;

namespace FBXSharp.Objective
{
    public class AnimationCurve : FBXObject
    {
        private long[] m_keyTimes;
        private float[] m_keyValues;

        public static readonly FBXObjectType FType = FBXObjectType.AnimationCurve;

        public static readonly FBXClassType FClass = FBXClassType.AnimationCurve;

        public override FBXObjectType Type => FType;

        public override FBXClassType Class => FClass;

        public long[] KeyTimes
        {
            get => m_keyTimes;
            set => m_keyTimes = value ?? Array.Empty<long>();
        }

        public float[] KeyValues
        {
            get => m_keyValues;
            set => m_keyValues = value ?? Array.Empty<float>();
        }

        internal AnimationCurve(IElement element, IScene scene) : base(element, scene)
        {
            m_keyTimes = Array.Empty<long>();
            m_keyValues = Array.Empty<float>();

            if (element is null) return;

            var times = element.FindChild("KeyTime");
            var value = element.FindChild("KeyValueFloat");

            if (!(times is null) && times.Attributes.Length > 0 &&
                times.Attributes[0].GetElementValue() is Array timesArray)
            {
                m_keyTimes = new long[timesArray.Length];
                Array.Copy(timesArray, m_keyTimes, m_keyTimes.Length);
            }

            if (!(value is null) && value.Attributes.Length > 0 &&
                value.Attributes[0].GetElementValue() is Array valueArray)
            {
                m_keyValues = new float[valueArray.Length];
                Array.Copy(valueArray, m_keyValues, m_keyValues.Length);
            }

            if (m_keyValues.Length != m_keyTimes.Length)
                throw new Exception($"Invalid animation curve with name {Name}");
        }

        public override IElement AsElement(bool binary)
        {
            return new Element(Class.ToString(), null, BuildAttributes("AnimCurve", string.Empty, binary)); // #TODO
        }
    }
}