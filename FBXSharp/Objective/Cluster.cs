using System;
using FBXSharp.Core;
using FBXSharp.ValueTypes;

namespace FBXSharp.Objective
{
    public class Cluster : FBXObject
    {
        private double[] m_weights;
        private int[] m_indices;
        private Model m_link;

        public static readonly FBXObjectType FType = FBXObjectType.Cluster;

        public static readonly FBXClassType FClass = FBXClassType.Deformer;

        public override FBXObjectType Type => FType;

        public override FBXClassType Class => FClass;

        public int[] Indices => m_indices;

        public double[] Weights => m_weights;

        public Matrix4x4 Transform { get; set; }

        public Matrix4x4 TransformLink { get; set; }

        public Matrix4x4 TransformAssociateModel { get; set; }

        public Model Link
        {
            get => m_link;
            set => InternalSetLink(value);
        }

        internal Cluster(IElement element, IScene scene) : base(element, scene)
        {
            m_indices = Array.Empty<int>();
            m_weights = Array.Empty<double>();
            Transform = Matrix4x4.Identity;
            TransformLink = Matrix4x4.Identity;
            TransformAssociateModel = Matrix4x4.Identity;

            if (element is null) return;

            var indexes = element.FindChild("Indexes");

            if (!(indexes is null) && indexes.Attributes.Length > 0 &&
                indexes.Attributes[0].Type == IElementAttributeType.ArrayInt32)
                _ = ElementaryFactory.ToInt32Array(indexes.Attributes[0], out m_indices);

            var weights = element.FindChild("Weights");

            if (!(weights is null) && weights.Attributes.Length > 0 &&
                weights.Attributes[0].Type == IElementAttributeType.ArrayDouble)
                _ = ElementaryFactory.ToDoubleArray(weights.Attributes[0], out m_weights);

            var transform = element.FindChild("Transform");

            if (!(transform is null) && transform.Attributes.Length > 0 &&
                transform.Attributes[0].Type == IElementAttributeType.ArrayDouble)
                if (ElementaryFactory.ToMatrix4x4(transform.Attributes[0], out var matrix))
                    Transform = matrix;

            var transformLink = element.FindChild("TransformLink");

            if (!(transformLink is null) && transformLink.Attributes.Length > 0 &&
                transformLink.Attributes[0].Type == IElementAttributeType.ArrayDouble)
                if (ElementaryFactory.ToMatrix4x4(transformLink.Attributes[0], out var matrix))
                    TransformLink = matrix;

            var transformAssociateModel = element.FindChild("TransformAssociateModel");

            if (!(transformAssociateModel is null) && transformAssociateModel.Attributes.Length > 0 &&
                transformAssociateModel.Attributes[0].Type == IElementAttributeType.ArrayDouble)
                if (ElementaryFactory.ToMatrix4x4(transformAssociateModel.Attributes[0], out var matrix))
                    TransformAssociateModel = matrix;
        }

        private void InternalSetLink(Model model)
        {
            if (model is null)
            {
                m_link = null;
                return;
            }

            if (model.Scene != Scene) throw new Exception("Model should share same scene with cluster");

            if (model.Type != FBXObjectType.Null && model.Type != FBXObjectType.LimbNode &&
                model.Type != FBXObjectType.Mesh)
                throw new Exception("Model linked should be either null node, limb node, or mesh node");

            m_link = model;
        }

        public void SetupBlendWeights(int[] indices, double[] weights)
        {
            if (indices is null && weights is null)
            {
                m_indices = Array.Empty<int>();
                m_weights = Array.Empty<double>();

                return;
            }

            if (indices.Length != weights.Length)
                throw new Exception("Indices and weights arrays should all have the same length");

            m_indices = indices;
            m_weights = weights;
        }

        public override Connection[] GetConnections()
        {
            if (m_link is null)
                return Array.Empty<Connection>();
            return new[]
            {
                new Connection(Connection.ConnectionType.Object, m_link.GetHashCode(), GetHashCode())
            };
        }

        public override void ResolveLink(FBXObject linker, IElementAttribute attribute)
        {
            if (linker.Class == FBXClassType.Model) InternalSetLink(linker as Model);
        }

        public override IElement AsElement(bool binary)
        {
            var hasAnyProperties = Properties.Count != 0;

            var elements = new IElement[7 + (hasAnyProperties ? 1 : 0)];

            elements[0] = Element.WithAttribute("Version", ElementaryFactory.GetElementAttribute(100));
            elements[2] = Element.WithAttribute("Indexes", ElementaryFactory.GetElementAttribute(m_indices));
            elements[3] = Element.WithAttribute("Weights", ElementaryFactory.GetElementAttribute(m_weights));
            elements[4] = Element.WithAttribute("Transform", ElementaryFactory.GetElementAttribute(Transform));
            elements[5] = Element.WithAttribute("TransformLink", ElementaryFactory.GetElementAttribute(TransformLink));
            elements[6] = Element.WithAttribute("TransformAssociateModel",
                ElementaryFactory.GetElementAttribute(TransformAssociateModel));

            elements[1] = new Element("UserData", null, new[]
            {
                ElementaryFactory.GetElementAttribute(string.Empty),
                ElementaryFactory.GetElementAttribute(string.Empty)
            });

            if (hasAnyProperties) elements[7] = BuildProperties70();

            return new Element(Class.ToString(), elements, BuildAttributes("SubDeformer", Type.ToString(), binary));
        }
    }
}