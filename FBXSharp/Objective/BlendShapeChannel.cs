using System;
using System.Collections.Generic;
using FBXSharp.Core;

namespace FBXSharp.Objective
{
    public class BlendShapeChannel : FBXObject
    {
        private readonly List<Shape> m_shapes;
        private double[] m_fullWeights;

        public static readonly FBXObjectType FType = FBXObjectType.BlendShapeChannel;

        public static readonly FBXClassType FClass = FBXClassType.Deformer;

        public override FBXObjectType Type => FType;

        public override FBXClassType Class => FClass;

        public double DeformPercent { get; set; }

        public double[] FullWeights
        {
            get => m_fullWeights;
            set => m_fullWeights = value ?? Array.Empty<double>();
        }

        public IReadOnlyList<Shape> Shapes => m_shapes;

        internal BlendShapeChannel(IElement element, IScene scene) : base(element, scene)
        {
            m_shapes = new List<Shape>();
            m_fullWeights = Array.Empty<double>();

            if (element is null) return;

            var percent = element.FindChild("DeformPercent");

            if (!(percent is null) && percent.Attributes.Length > 0 &&
                percent.Attributes[0].Type == IElementAttributeType.Double)
                DeformPercent = Convert.ToDouble(percent.Attributes[0].GetElementValue());

            var weights = element.FindChild("FullWeights");

            if (!(weights is null) && weights.Attributes.Length > 0 &&
                weights.Attributes[0].Type == IElementAttributeType.ArrayDouble)
                _ = ElementaryFactory.ToDoubleArray(weights.Attributes[0], out m_fullWeights);
        }

        public void AddShape(Shape shape)
        {
            if (shape is null) return;

            if (shape.Scene != Scene) throw new Exception("Shape should share same scene with blend shape channel");

            m_shapes.Add(shape);
        }

        public void RemoveShape(Shape shape)
        {
            if (shape is null || shape.Scene != Scene) return;

            _ = m_shapes.Remove(shape);
        }

        public void AddShapeAt(Shape shape, int index)
        {
            if (shape is null) return;

            if (shape.Scene != Scene) throw new Exception("Shape should share same scene with blend shape channel");

            if (index < 0 || index > m_shapes.Count)
                throw new ArgumentOutOfRangeException("Index should be in range 0 to shape count inclusively");

            m_shapes.Insert(index, shape);
        }

        public void RemoveMaterialAt(int index)
        {
            if (index < 0 || index >= m_shapes.Count)
                throw new ArgumentOutOfRangeException("Index should be in 0 to shape count range");

            m_shapes.RemoveAt(index);
        }

        public override Connection[] GetConnections()
        {
            if (m_shapes.Count == 0)
            {
                return Array.Empty<Connection>();
            }

            var thisHashKey = GetHashCode();
            var connections = new Connection[m_shapes.Count];

            for (var i = 0; i < connections.Length; ++i)
                connections[i] = new Connection(Connection.ConnectionType.Object, m_shapes[i].GetHashCode(),
                    thisHashKey);

            return connections;
        }

        public override void ResolveLink(FBXObject linker, IElementAttribute attribute)
        {
            if (linker.Class == FBXClassType.Geometry && linker.Type == FBXObjectType.Shape) AddShape(linker as Shape);
        }

        public override IElement AsElement(bool binary)
        {
            var elements = new IElement[3];

            elements[0] = Element.WithAttribute("Version", ElementaryFactory.GetElementAttribute(100));
            elements[1] = Element.WithAttribute("DeformPercent", ElementaryFactory.GetElementAttribute(DeformPercent));
            elements[2] = Element.WithAttribute("FullWeights", ElementaryFactory.GetElementAttribute(m_fullWeights));

            return new Element(Class.ToString(), elements,
                BuildAttributes("SubDeformer", Type.ToString(), binary)); // #TODO
        }
    }
}