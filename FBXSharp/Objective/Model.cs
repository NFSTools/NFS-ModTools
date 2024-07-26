using System;
using System.Collections.Generic;
using FBXSharp.Core;
using FBXSharp.ValueTypes;

namespace FBXSharp.Objective
{
    public abstract class Model : FBXObject
    {
        public enum ShadingType
        {
            HardShading,
            WireFrame,
            FlatShading,
            LightShading,
            TextureShading,
            FullShading
        }

        public enum CullingType
        {
            CullingOff,
            CullingOnCCW,
            CullingOnCW
        }

        public enum InheritanceType
        {
            InheritRrSs,
            InheritRSrs,
            InheritRrs
        }

        private readonly List<Model> m_children;
        private NodeAttribute m_attribute;

        public static readonly FBXClassType FClass = FBXClassType.Model;

        public override FBXClassType Class => FClass;

        public abstract bool SupportsAttribute { get; }

        public NodeAttribute Attribute
        {
            get => InternalGetNodeAttribute();
            set => InternalSetNodeAttribute(value);
        }

        public Model Parent { get; private set; }

        public IReadOnlyList<Model> Children => m_children;

        public ShadingType Shading { get; set; }

        public CullingType Culling { get; set; }

        public Enumeration RotationOrder
        {
            get => InternalGetEnumeration(nameof(RotationOrder));
            set => InternalSetEnumeration(nameof(RotationOrder), value, "enum", string.Empty);
        }

        public Vector3? RotationOffset
        {
            get => InternalGetPrimitive<Vector3>(nameof(RotationOffset), IElementPropertyType.Double3);
            set => InternalSetPrimitive(nameof(RotationOffset), IElementPropertyType.Double3, value, "Vector3D",
                "Vector3D", IElementPropertyFlags.Animatable);
        }

        public Vector3? RotationPivot
        {
            get => InternalGetPrimitive<Vector3>(nameof(RotationPivot), IElementPropertyType.Double3);
            set => InternalSetPrimitive(nameof(RotationPivot), IElementPropertyType.Double3, value, "Vector3D",
                "Vector3D", IElementPropertyFlags.Animatable);
        }

        public Vector3? PreRotation
        {
            get => InternalGetPrimitive<Vector3>(nameof(PreRotation), IElementPropertyType.Double3);
            set => InternalSetPrimitive(nameof(PreRotation), IElementPropertyType.Double3, value, "Vector3D",
                "Vector3D", IElementPropertyFlags.Animatable);
        }

        public Vector3? PostRotation
        {
            get => InternalGetPrimitive<Vector3>(nameof(PostRotation), IElementPropertyType.Double3);
            set => InternalSetPrimitive(nameof(PostRotation), IElementPropertyType.Double3, value, "Vector3D",
                "Vector3D", IElementPropertyFlags.Animatable);
        }

        public Vector3? ScalingOffset
        {
            get => InternalGetPrimitive<Vector3>(nameof(ScalingOffset), IElementPropertyType.Double3);
            set => InternalSetPrimitive(nameof(ScalingOffset), IElementPropertyType.Double3, value, "Vector3D",
                "Vector3D", IElementPropertyFlags.Animatable);
        }

        public Vector3? ScalingPivot
        {
            get => InternalGetPrimitive<Vector3>(nameof(ScalingPivot), IElementPropertyType.Double3);
            set => InternalSetPrimitive(nameof(ScalingPivot), IElementPropertyType.Double3, value, "Vector3D",
                "Vector3D", IElementPropertyFlags.Animatable);
        }

        public Vector3? LocalTranslation
        {
            get => InternalGetPrimitive<Vector3>("Lcl Translation", IElementPropertyType.Double3);
            set => InternalSetPrimitive("Lcl Translation", IElementPropertyType.Double3, value, "Lcl Translation",
                "Lcl Translation", IElementPropertyFlags.Animatable);
        }

        public Vector3? LocalRotation
        {
            get => InternalGetPrimitive<Vector3>("Lcl Rotation", IElementPropertyType.Double3);
            set => InternalSetPrimitive("Lcl Rotation", IElementPropertyType.Double3, value, "Lcl Rotation",
                "Lcl Rotation", IElementPropertyFlags.Animatable);
        }

        public Vector3? LocalScale
        {
            get => InternalGetPrimitive<Vector3>("Lcl Scaling", IElementPropertyType.Double3);
            set => InternalSetPrimitive("Lcl Scaling", IElementPropertyType.Double3, value, "Lcl Scaling",
                "Lcl Scaling", IElementPropertyFlags.Animatable);
        }

        public double? Visibility
        {
            get => InternalGetPrimitive<double>(nameof(Visibility), IElementPropertyType.Double);
            set => InternalSetPrimitive(nameof(Visibility), IElementPropertyType.Double, value, "Visibility",
                string.Empty, IElementPropertyFlags.Animatable);
        }

        public bool? VisibilityInheritance
        {
            get => InternalGetPrimitive<bool>("Visibility Inheritance", IElementPropertyType.Bool);
            set => InternalSetPrimitive("Visibility Inheritance", IElementPropertyType.Bool, value,
                "Visibility Inheritance", string.Empty);
        }

        public int? DefaultAttributeIndex
        {
            get => InternalGetPrimitive<int>(nameof(DefaultAttributeIndex), IElementPropertyType.Int);
            set => InternalSetPrimitive(nameof(DefaultAttributeIndex), IElementPropertyType.Int, value, "int",
                "Integer");
        }

        public InheritanceType? InheritType
        {
            get => InternalGetEnumType(nameof(InheritType), out InheritanceType type) ? type : (InheritanceType?)null;
            set => InternalSetEnumType(nameof(InheritType), value.HasValue, (int)(value ?? 0), "enum", string.Empty);
        }

        internal Model(IElement element, IScene scene) : base(element, scene)
        {
            m_children = new List<Model>();
            ParseDepthFields(element);
        }

        private void ParseDepthFields(IElement element)
        {
            if (element is null) return;

            var shading = element.FindChild(nameof(Shading));
            var culling = element.FindChild(nameof(Culling));

            if (!(shading is null) && shading.Attributes.Length > 0)
            {
                var type = (char)Convert.ToByte(shading.Attributes[0].GetElementValue());

                switch (type)
                {
                    case 'W':
                        Shading = ShadingType.WireFrame;
                        break;
                    case 'F':
                        Shading = ShadingType.FlatShading;
                        break;
                    case 'Y':
                        Shading = ShadingType.LightShading;
                        break;
                    case 'T':
                        Shading = ShadingType.TextureShading;
                        break;
                    case 'U':
                        Shading = ShadingType.FullShading;
                        break;
                    default:
                        Shading = ShadingType.HardShading;
                        break;
                }
            }

            if (!(culling is null) && culling.Attributes.Length > 0)
                if (Enum.TryParse(culling.Attributes[0].GetElementValue().ToString(), out CullingType type))
                    Culling = type;
        }

        private NodeAttribute InternalGetNodeAttribute()
        {
            if (!SupportsAttribute) throw new NotSupportedException("Model does not support node attributes");

            return m_attribute;
        }

        private void InternalSetNodeAttribute(NodeAttribute attribute)
        {
            if (!SupportsAttribute) throw new NotSupportedException("Model does not support node attributes");

            if (attribute.Type != Type) throw new Exception("Node attribute should have same type as the Model");

            m_attribute = attribute;
        }

        internal void InternalSetChild(Model child)
        {
            m_children.Add(child);
            child.InternalSetParent(this);
        }

        internal void InternalSetParent(Model parent)
        {
            Parent = parent;
        }

        private Matrix4x4 EvaluateLocal(in Vector3 position, in Vector3 rotation)
        {
            var scale = LocalScale;

            return EvaluateLocal(position, rotation, scale.HasValue ? scale.Value : Vector3.One);
        }

        private Matrix4x4 EvaluateLocal(in Vector3 position, in Vector3 rotation, in Vector3 scale)
        {
            var rotationPivot = RotationPivot.GetValueOrDefault();
            var scalingPivot = ScalingPivot.GetValueOrDefault();
            var rotationOrder = (RotationOrder)(RotationOrder?.Value ?? 0);

            var t = Matrix4x4.CreateTranslation(position);
            var s = Matrix4x4.CreateScale(scale);
            var r = Matrix4x4.CreateFromEuler(rotation, rotationOrder);

            var rpre = Matrix4x4.CreateFromEuler(PreRotation.GetValueOrDefault());
            var post = Matrix4x4.CreateFromEuler(-PostRotation.GetValueOrDefault(), ValueTypes.RotationOrder.ZYX);

            var roff = Matrix4x4.CreateTranslation(RotationOffset.GetValueOrDefault());
            var rpip = Matrix4x4.CreateTranslation(rotationPivot);
            var rpii = Matrix4x4.CreateTranslation(-rotationPivot);

            var soff = Matrix4x4.CreateTranslation(ScalingOffset.GetValueOrDefault());
            var spip = Matrix4x4.CreateTranslation(scalingPivot);
            var spii = Matrix4x4.CreateTranslation(-scalingPivot);

            return t * roff * rpip * rpre * r * post * rpii * soff * spip * s * spii;
        }

        protected IElement MakeElement(string className, bool binary)
        {
            var elements = new IElement[6];

            byte shading = 0;

            switch (Shading)
            {
                case ShadingType.WireFrame:
                    shading = (byte)'W';
                    break;
                case ShadingType.FlatShading:
                    shading = (byte)'F';
                    break;
                case ShadingType.LightShading:
                    shading = (byte)'Y';
                    break;
                case ShadingType.TextureShading:
                    shading = (byte)'T';
                    break;
                case ShadingType.FullShading:
                    shading = (byte)'U';
                    break;
            }

            elements[0] = Element.WithAttribute("Version", ElementaryFactory.GetElementAttribute(232));
            elements[1] = BuildProperties70();
            elements[2] = Element.WithAttribute("MultiLayer", ElementaryFactory.GetElementAttribute(false));
            elements[3] = Element.WithAttribute("MultiTake", ElementaryFactory.GetElementAttribute(0));
            elements[4] = Element.WithAttribute("Shading", ElementaryFactory.GetElementAttribute(shading));
            elements[5] = Element.WithAttribute("Culling", ElementaryFactory.GetElementAttribute(Culling.ToString()));

            return new Element(Class.ToString(), elements, BuildAttributes(className, Type.ToString(), binary));
        }

        public Matrix4x4 GetLocalTransform()
        {
            var translation = LocalTranslation;
            var rotation = LocalRotation;
            var scale = LocalScale;

            return EvaluateLocal(translation.GetValueOrDefault(), rotation.GetValueOrDefault(),
                scale.HasValue ? scale.Value : Vector3.One);
        }

        public Matrix4x4 GetGlobalTransform()
        {
            if (Parent is null)
                return EvaluateLocal(LocalTranslation.GetValueOrDefault(), LocalRotation.GetValueOrDefault());
            return Parent.GetGlobalTransform() *
                   EvaluateLocal(LocalTranslation.GetValueOrDefault(), LocalRotation.GetValueOrDefault());
        }

        public void AddChild(Model model)
        {
            if (model is null) return;

            if (model.Scene != Scene)
                throw new ArgumentException("Model passed should share same scene with the current model");

            if (ReferenceEquals(this, model)) throw new Exception("Cannot add itself as a child");

            if (model.Parent is null)
            {
                InternalSetChild(model);
            }
            else if (model.Parent != this)
            {
                model.DetachFromParent();
                InternalSetChild(model);
            }
        }

        public void RemoveChild(Model model)
        {
            if (model is null || model.Scene != Scene || model.Parent != this) return;

            _ = m_children.Remove(model);
            model.Parent = null;
        }

        public void AddChildAt(Model model, int index)
        {
            if (model is null) return;

            if (model.Scene != Scene)
                throw new ArgumentException("Model passed should share same scene with the current model");

            if (ReferenceEquals(this, model)) throw new Exception("Cannot add itself as a child");

            if (index < 0 || index > m_children.Count)
                throw new ArgumentOutOfRangeException("Index should be in range 0 to children count inclusively");

            if (model.Parent is null)
            {
                m_children.Insert(index, model);
                model.Parent = this;
            }
            else if (model.Parent != this)
            {
                model.DetachFromParent();
                m_children.Insert(index, model);
                model.Parent = this;
            }
        }

        public void RemoveChildAt(int index)
        {
            if (index < 0 || index >= m_children.Count)
                throw new ArgumentOutOfRangeException("Index should be in 0 to children count range");

            var model = m_children[index];
            m_children.RemoveAt(index);
            model.Parent = null;
        }

        public void DetachFromParent()
        {
            if (Parent is null) return;

            _ = Parent.m_children.Remove(this);
            Parent = null;
        }

        public void DetachAllChildren()
        {
            foreach (var child in m_children) child.Parent = null;

            m_children.Clear();
        }

        public override Connection[] GetConnections()
        {
            if (m_children.Count == 0)
            {
                if (!SupportsAttribute || Attribute is null)
                    return Array.Empty<Connection>();
                return new Connection[1]
                {
                    new Connection(Connection.ConnectionType.Object, Attribute.GetHashCode(), GetHashCode())
                };
            }

            var attributeOn = SupportsAttribute && !(Attribute is null);
            var connections = new Connection[m_children.Count + (attributeOn ? 1 : 0)];

            for (var i = 0; i < m_children.Count; ++i)
                connections[i] = new Connection(Connection.ConnectionType.Object, m_children[i].GetHashCode(),
                    GetHashCode());

            if (attributeOn)
                connections[m_children.Count] = new Connection(Connection.ConnectionType.Object,
                    Attribute.GetHashCode(), GetHashCode());

            return connections;
        }

        public override void ResolveLink(FBXObject linker, IElementAttribute attribute)
        {
            if (linker.Class == FBXClassType.Model)
            {
                AddChild(linker as Model);

                return;
            }

            if (linker.Class == FBXClassType.NodeAttribute) InternalSetNodeAttribute(linker as NodeAttribute);
        }

        public override void Destroy()
        {
            DetachAllChildren();
            base.Destroy();
        }
    }

    public abstract class NodeAttribute : FBXObject
    {
        public static readonly FBXClassType FClass = FBXClassType.NodeAttribute;

        public override FBXClassType Class => FClass;

        internal NodeAttribute(IElement element, IScene scene) : base(element, scene)
        {
        }
    }
}