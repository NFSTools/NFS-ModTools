using System;
using System.Collections.Generic;
using FBXSharp.Core;
using FBXSharp.ValueTypes;

namespace FBXSharp.Objective
{
    public class Material : FBXObject
    {
        public struct Channel
        {
            public string Name { get; }
            public Texture Texture { get; }

            public Channel(string name, Texture texture)
            {
                Name = name ?? string.Empty;
                Texture = texture;
            }

            public override string ToString()
            {
                return Name;
            }
        }

        private readonly List<Channel> m_channels;
        private string m_shadingModel;

        public static readonly FBXObjectType FType = FBXObjectType.Material;

        public static readonly FBXClassType FClass = FBXClassType.Material;

        public override FBXObjectType Type => FType;

        public override FBXClassType Class => FClass;

        public IReadOnlyList<Channel> Channels => m_channels;

        public string ShadingModel
        {
            get => m_shadingModel;
            set => m_shadingModel = value ?? string.Empty;
        }

        public bool MultiLayer { get; set; }

        public ColorRGB? DiffuseColor
        {
            get => InternalGetColor(nameof(DiffuseColor));
            set => InternalSetColor(nameof(DiffuseColor), value, "Color", string.Empty,
                IElementPropertyFlags.Animatable);
        }

        public ColorRGB? SpecularColor
        {
            get => InternalGetColor(nameof(SpecularColor));
            set => InternalSetColor(nameof(SpecularColor), value, "Color", string.Empty,
                IElementPropertyFlags.Animatable);
        }

        public ColorRGB? ReflectionColor
        {
            get => InternalGetColor(nameof(ReflectionColor));
            set => InternalSetColor(nameof(ReflectionColor), value, "Color", string.Empty,
                IElementPropertyFlags.Animatable);
        }

        public ColorRGB? AmbientColor
        {
            get => InternalGetColor(nameof(AmbientColor));
            set => InternalSetColor(nameof(AmbientColor), value, "Color", string.Empty,
                IElementPropertyFlags.Animatable);
        }

        public ColorRGB? EmissiveColor
        {
            get => InternalGetColor(nameof(EmissiveColor));
            set => InternalSetColor(nameof(EmissiveColor), value, "Color", string.Empty,
                IElementPropertyFlags.Animatable);
        }

        public ColorRGB? TransparentColor
        {
            get => InternalGetColor(nameof(TransparentColor));
            set => InternalSetColor(nameof(TransparentColor), value, "Color", string.Empty,
                IElementPropertyFlags.Animatable);
        }

        public double? DiffuseFactor
        {
            get => InternalGetPrimitive<double>(nameof(DiffuseFactor), IElementPropertyType.Double);
            set => InternalSetPrimitive(nameof(DiffuseFactor), IElementPropertyType.Double, value, "Number",
                string.Empty, IElementPropertyFlags.Animatable);
        }

        public double? SpecularFactor
        {
            get => InternalGetPrimitive<double>(nameof(SpecularFactor), IElementPropertyType.Double);
            set => InternalSetPrimitive(nameof(SpecularFactor), IElementPropertyType.Double, value, "Number",
                string.Empty, IElementPropertyFlags.Animatable);
        }

        public double? ReflectionFactor
        {
            get => InternalGetPrimitive<double>(nameof(ReflectionFactor), IElementPropertyType.Double);
            set => InternalSetPrimitive(nameof(ReflectionFactor), IElementPropertyType.Double, value, "Number",
                string.Empty, IElementPropertyFlags.Animatable);
        }

        public double? Shininess
        {
            get => InternalGetPrimitive<double>(nameof(Shininess), IElementPropertyType.Double);
            set => InternalSetPrimitive(nameof(Shininess), IElementPropertyType.Double, value, "Number", string.Empty,
                IElementPropertyFlags.Animatable);
        }

        public double? ShininessExponent
        {
            get => InternalGetPrimitive<double>(nameof(ShininessExponent), IElementPropertyType.Double);
            set => InternalSetPrimitive(nameof(ShininessExponent), IElementPropertyType.Double, value, "Number",
                string.Empty, IElementPropertyFlags.Animatable);
        }

        public double? AmbientFactor
        {
            get => InternalGetPrimitive<double>(nameof(AmbientFactor), IElementPropertyType.Double);
            set => InternalSetPrimitive(nameof(AmbientFactor), IElementPropertyType.Double, value, "Number",
                string.Empty, IElementPropertyFlags.Animatable);
        }

        public double? BumpFactor
        {
            get => InternalGetPrimitive<double>(nameof(BumpFactor), IElementPropertyType.Double);
            set => InternalSetPrimitive(nameof(BumpFactor), IElementPropertyType.Double, value, "double", "Number",
                IElementPropertyFlags.Animatable);
        }

        public double? EmissiveFactor
        {
            get => InternalGetPrimitive<double>(nameof(EmissiveFactor), IElementPropertyType.Double);
            set => InternalSetPrimitive(nameof(EmissiveFactor), IElementPropertyType.Double, value, "Number",
                string.Empty, IElementPropertyFlags.Animatable);
        }

        public double? TransparencyFactor
        {
            get => InternalGetPrimitive<double>(nameof(TransparencyFactor), IElementPropertyType.Double);
            set => InternalSetPrimitive(nameof(TransparencyFactor), IElementPropertyType.Double, value, "Number",
                string.Empty, IElementPropertyFlags.Animatable);
        }

        internal Material(IElement element, IScene scene) : base(element, scene)
        {
            m_channels = new List<Channel>();
            m_shadingModel = "Phong";
            MultiLayer = false; // ??

            if (element is null) return;

            var shading = element.FindChild("ShadingModel");

            if (!(shading is null) && shading.Attributes.Length > 0 &&
                shading.Attributes[0].Type == IElementAttributeType.String)
                m_shadingModel = shading.Attributes[0].GetElementValue().ToString() ?? string.Empty;

            var multi = element.FindChild("MultiLayer");

            if (!(multi is null) && multi.Attributes.Length > 0)
                MultiLayer = Convert.ToBoolean(multi.Attributes[0].GetElementValue());
        }

        internal void InternalSetChannel(in Channel channel)
        {
            m_channels.Add(channel);
        }

        public void AddChannel(Channel channel)
        {
            AddChannelAt(channel, m_channels.Count);
        }

        public void RemoveChannel(Channel channel)
        {
            m_channels.Remove(channel);
        }

        public void AddChannelAt(Channel channel, int index)
        {
            if (string.IsNullOrEmpty(channel.Name))
                throw new ArgumentNullException("Channel name cannot be null or empty");

            if (channel.Texture is null) return;

            if (channel.Texture.Scene != Scene)
                throw new Exception("Texture in the channel passed should share same scene with material");

            if (index < 0 || index > m_channels.Count)
                throw new ArgumentOutOfRangeException("Index should be in range 0 to channel count inclusively");

            if (m_channels.FindIndex(_ => _.Name == channel.Name) >= 0) return;

            m_channels.Insert(index, channel);
        }

        public void RemoveChannelAt(int index)
        {
            if (index < 0 || index >= m_channels.Count)
                throw new ArgumentOutOfRangeException("Index should be in 0 to channel count range");

            m_channels.RemoveAt(index);
        }

        public override Connection[] GetConnections()
        {
            if (m_channels.Count == 0) return Array.Empty<Connection>();

            var connections = new Connection[m_channels.Count];

            for (var i = 0; i < connections.Length; ++i)
                connections[i] = new Connection
                (
                    Connection.ConnectionType.Property,
                    m_channels[i].Texture.GetHashCode(),
                    GetHashCode(),
                    ElementaryFactory.GetElementAttribute(m_channels[i].Name)
                );

            return connections;
        }

        public override void ResolveLink(FBXObject linker, IElementAttribute attribute)
        {
            if (linker.Class == FBXClassType.Texture && linker.Type == FBXObjectType.Texture)
            {
                if (attribute is null || attribute.Type != IElementAttributeType.String) return;

                AddChannel(new Channel(attribute.GetElementValue().ToString(), linker as Texture));
            }
        }

        public override IElement AsElement(bool binary)
        {
            var elements = new IElement[4];

            elements[0] = Element.WithAttribute("Version", ElementaryFactory.GetElementAttribute(102));
            elements[1] = Element.WithAttribute("ShadingModel", ElementaryFactory.GetElementAttribute(m_shadingModel));
            elements[2] =
                Element.WithAttribute("MultiLayer", ElementaryFactory.GetElementAttribute(MultiLayer ? 1 : 0));
            elements[3] = BuildProperties70();

            return new Element(Class.ToString(), elements, BuildAttributes("Material", string.Empty, binary));
        }
    }

    public class MaterialBuilder : BuilderBase
    {
        public enum ColorType
        {
            DiffuseColor,
            SpecularColor,
            ReflectionColor,
            AmbientColor,
            EmissiveColor,
            TransparentColor
        }

        public enum FactorType
        {
            DiffuseFactor,
            SpecularFactor,
            ReflectionFactor,
            Shininess,
            ShininessExponent,
            AmbientFactor,
            BumpFactor,
            EmissiveFactor,
            TransparencyFactor
        }

        public enum ChannelType
        {
            DiffuseColor,
            NormalMap,
            HeightMap,
            OcclusionMap,
            ReflectionColor,
            AmbientColor,
            EmissiveColor,
            SpecularColor,
            TransparentColor
        }

        private readonly List<Material.Channel> m_channels;
        private string m_shadingModel;
        private bool m_multiLayer;

        public IReadOnlyList<Material.Channel> Channels => m_channels;

        public MaterialBuilder(Scene scene) : base(scene)
        {
            m_channels = new List<Material.Channel>();
            m_shadingModel = "Phong";
        }

        public Material BuildMaterial()
        {
            var material = m_scene.CreateMaterial();

            material.Name = m_name;
            material.ShadingModel = m_shadingModel;
            material.MultiLayer = m_multiLayer;

            foreach (var channel in m_channels) material.InternalSetChannel(channel);

            foreach (var property in m_properties) material.AddProperty(property);

            return material;
        }

        public MaterialBuilder WithName(string name)
        {
            SetObjectName(name);
            return this;
        }

        public MaterialBuilder WithFBXProperty<T>(string name, T value, bool isUser = false)
        {
            SetFBXProperty(name, value, isUser);
            return this;
        }

        public MaterialBuilder WithFBXProperty<T>(string name, T value, IElementPropertyFlags flags)
        {
            SetFBXProperty(name, value, flags);
            return this;
        }

        public MaterialBuilder WithFBXProperty<T>(FBXProperty<T> property)
        {
            SetFBXProperty(property);
            return this;
        }

        public MaterialBuilder WithShadingModel(string shading)
        {
            m_shadingModel = shading ?? "Phong";
            return this;
        }

        public MaterialBuilder WithMultiLayer(bool multiLayer)
        {
            m_multiLayer = multiLayer;
            return this;
        }

        public MaterialBuilder WithChannel(ChannelType type, Texture texture)
        {
            return WithChannel(type.ToString(), texture);
        }

        public MaterialBuilder WithChannel(string name, Texture texture)
        {
            return WithChannel(new Material.Channel(name, texture));
        }

        public MaterialBuilder WithChannel(in Material.Channel channel)
        {
            if (channel.Texture is null)
                throw new ArgumentNullException($"Channel {channel.Name} cannot have null texture");

            if (channel.Texture.Scene != m_scene) throw new Exception("Texture should share same scene with material");

            m_channels.Add(channel);
            return this;
        }

        public MaterialBuilder WithColor(ColorType type, ColorRGB color)
        {
            return WithColor(type.ToString(), color);
        }

        public MaterialBuilder WithColor(string name, ColorRGB color)
        {
            var flag = IElementPropertyFlags.Imported | IElementPropertyFlags.Animatable;
            var prop = new FBXProperty<Vector3>("Color", string.Empty, name, flag, color);

            SetFBXProperty(prop);
            return this;
        }

        public MaterialBuilder WithFactor(FactorType type, double factor)
        {
            return WithFactor(type.ToString(), factor);
        }

        public MaterialBuilder WithFactor(string name, double factor)
        {
            var flag = IElementPropertyFlags.Imported | IElementPropertyFlags.Animatable;
            var p2nd = name.StartsWith("Bump") ? "Number" : string.Empty;
            var p1st = string.IsNullOrEmpty(p2nd) ? "Number" : "double";
            var prop = new FBXProperty<double>(p1st, p2nd, name, flag, factor);

            SetFBXProperty(prop);
            return this;
        }
    }
}