using System;
using FBXSharp.Core;

namespace FBXSharp.Objective
{
    public class AnimationCurveNode : FBXObject
    {
        private AnimationCurve m_curveX;
        private AnimationCurve m_curveY;
        private AnimationCurve m_curveZ;

        public static readonly FBXObjectType FType = FBXObjectType.AnimationCurveNode;

        public static readonly FBXClassType FClass = FBXClassType.AnimationCurveNode;

        public override FBXObjectType Type => FType;

        public override FBXClassType Class => FClass;

        public double DeltaX { get; set; }
        public double DeltaY { get; set; }
        public double DeltaZ { get; set; }

        public AnimationCurve CurveX
        {
            get => m_curveX;
            set => InternalSetCurve(value, ref m_curveX);
        }

        public AnimationCurve CurveY
        {
            get => m_curveY;
            set => InternalSetCurve(value, ref m_curveY);
        }

        public AnimationCurve CurveZ
        {
            get => m_curveZ;
            set => InternalSetCurve(value, ref m_curveZ);
        }

        internal AnimationCurveNode(IElement element, IScene scene) : base(element, scene)
        {
        }

        private void InternalSetCurve(AnimationCurve curve, ref AnimationCurve target)
        {
            if (curve is null || curve.Scene == Scene)
            {
                target = curve;

                return;
            }

            throw new Exception("Animation curve should share same scene with animation curve node");
        }

        public override Connection[] GetConnections()
        {
            var noCurveX = m_curveX is null;
            var noCurveY = m_curveY is null;
            var noCurveZ = m_curveZ is null;

            if (noCurveX && noCurveY && noCurveZ) return Array.Empty<Connection>();

            var currentlyAt = 0;
            var thisHashKey = GetHashCode();
            var connections = new Connection[(noCurveX ? 0 : 1) + (noCurveY ? 0 : 1) + (noCurveZ ? 0 : 1)];

            if (!noCurveX)
                connections[currentlyAt++] = new Connection
                (
                    Connection.ConnectionType.Property,
                    m_curveX.GetHashCode(),
                    thisHashKey,
                    ElementaryFactory.GetElementAttribute("d|X")
                );

            if (!noCurveY)
                connections[currentlyAt++] = new Connection
                (
                    Connection.ConnectionType.Property,
                    m_curveY.GetHashCode(),
                    thisHashKey,
                    ElementaryFactory.GetElementAttribute("d|Y")
                );

            if (!noCurveZ)
                connections[currentlyAt++] = new Connection
                (
                    Connection.ConnectionType.Property,
                    m_curveZ.GetHashCode(),
                    thisHashKey,
                    ElementaryFactory.GetElementAttribute("d|Z")
                );

            return connections;
        }

        public override void ResolveLink(FBXObject linker, IElementAttribute attribute)
        {
            if (linker.Class == FBXClassType.AnimationCurve && linker.Type == FBXObjectType.AnimationCurve)
                if (attribute.Type == IElementAttributeType.String)
                    switch (attribute.GetElementValue().ToString())
                    {
                        case "d|X":
                            CurveX = linker as AnimationCurve;
                            return;
                        case "d|Y":
                            CurveY = linker as AnimationCurve;
                            return;
                        case "d|Z":
                            CurveZ = linker as AnimationCurve;
                            return;
                    }
        }

        public override IElement AsElement(bool binary)
        {
            var elements = Properties.Count == 0
                ? Array.Empty<IElement>()
                : new[] { BuildProperties70() };

            return new Element(Class.ToString(), elements, BuildAttributes("AnimCurveNode", string.Empty, binary));
        }
    }
}