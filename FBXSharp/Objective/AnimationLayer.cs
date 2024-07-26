using System;
using System.Collections.Generic;
using FBXSharp.Core;

namespace FBXSharp.Objective
{
    public class AnimationLayer : FBXObject
    {
        private readonly List<AnimationCurveNode> m_curves;

        public static readonly FBXObjectType FType = FBXObjectType.AnimationLayer;

        public static readonly FBXClassType FClass = FBXClassType.AnimationLayer;

        public override FBXObjectType Type => FType;

        public override FBXClassType Class => FClass;

        public IReadOnlyList<AnimationCurveNode> CurveNodes => m_curves;

        internal AnimationLayer(IElement element, IScene scene) : base(element, scene)
        {
            m_curves = new List<AnimationCurveNode>();
        }

        public override Connection[] GetConnections()
        {
            if (m_curves.Count == 0) return Array.Empty<Connection>();

            var thisHashKey = GetHashCode();
            var connections = new Connection[m_curves.Count];

            for (var i = 0; i < connections.Length; ++i)
                connections[i] = new Connection(Connection.ConnectionType.Object, m_curves[i].GetHashCode(),
                    thisHashKey);

            return connections;
        }

        public override void ResolveLink(FBXObject linker, IElementAttribute attribute)
        {
            if (linker.Class == FBXClassType.AnimationCurveNode && linker.Type == FBXObjectType.AnimationCurveNode)
                m_curves.Add(linker as AnimationCurveNode); // #TODO
        }

        public override IElement AsElement(bool binary)
        {
            var elements = Properties.Count == 0
                ? Array.Empty<IElement>()
                : new[] { BuildProperties70() };

            return new Element(Class.ToString(), elements, BuildAttributes("AnimLayer", string.Empty, binary));
        }
    }
}