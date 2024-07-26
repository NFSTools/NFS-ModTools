using System;
using System.Collections.Generic;
using FBXSharp.Core;

namespace FBXSharp.Objective
{
    public class AnimationStack : FBXObject
    {
        private readonly List<AnimationLayer> m_layers;

        public static readonly FBXObjectType FType = FBXObjectType.AnimationStack;

        public static readonly FBXClassType FClass = FBXClassType.AnimationStack;

        public override FBXObjectType Type => FType;

        public override FBXClassType Class => FClass;

        public IReadOnlyList<AnimationLayer> Layers => m_layers;

        internal AnimationStack(IElement element, IScene scene) : base(element, scene)
        {
            m_layers = new List<AnimationLayer>();
        }

        public override Connection[] GetConnections()
        {
            if (m_layers.Count == 0) return Array.Empty<Connection>();

            var thisHashKey = GetHashCode();
            var connections = new Connection[m_layers.Count];

            for (var i = 0; i < connections.Length; ++i)
                connections[i] = new Connection(Connection.ConnectionType.Object, m_layers[i].GetHashCode(),
                    thisHashKey);

            return connections;
        }

        public override void ResolveLink(FBXObject linker, IElementAttribute attribute)
        {
            if (linker.Class == FBXClassType.AnimationLayer && linker.Type == FBXObjectType.AnimationLayer)
                m_layers.Add(linker as AnimationLayer); // #TODO
        }

        public override IElement AsElement(bool binary)
        {
            var elements = Properties.Count == 0
                ? Array.Empty<IElement>()
                : new[] { BuildProperties70() };

            return new Element(Class.ToString(), elements, BuildAttributes("AnimStack", string.Empty, binary));
        }
    }
}