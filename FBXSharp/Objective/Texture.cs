using System;
using FBXSharp.Core;
using FBXSharp.ValueTypes;

namespace FBXSharp.Objective
{
    public class Texture : FBXObject
    {
        public enum AlphaSourceType
        {
            None,
            RGB_Intensity,
            Alpha_Black
        }

        public enum MappingType
        {
            Null,
            Planar,
            Spherical,
            Cylindrical,
            Box,
            Face,
            UV,
            Environment
        }

        public enum PlanarMappingNormalType
        {
            X,
            Y,
            Z
        }

        public enum TextureUseType
        {
            Standard,
            ShadowMap,
            LightMap,
            SphericalReflexionMap,
            SphereReflexionMap,
            BumpNormalMap
        }

        public enum WrapMode
        {
            Repeat,
            Clamp
        }

        public struct CropBase
        {
            public int V0 { get; set; }
            public int V1 { get; set; }
            public int V2 { get; set; }
            public int V3 { get; set; }

            public override string ToString()
            {
                return $"<{V0}, {V1}, {V2}, {V3}>";
            }
        }

        private Clip m_video;
        private string m_absolute;
        private string m_relative;
        private string m_media;
        private string m_textureName;

        public static readonly FBXObjectType FType = FBXObjectType.Texture;

        public static readonly FBXClassType FClass = FBXClassType.Texture;

        public override FBXObjectType Type => FType;

        public override FBXClassType Class => FClass;

        public Clip Data
        {
            get => m_video;
            set => InternalSetClip(value);
        }

        public string AbsolutePath
        {
            get => m_absolute;
            set => m_absolute = value ?? string.Empty;
        }

        public string RelativePath
        {
            get => m_relative;
            set => m_relative = value ?? string.Empty;
        }

        public string Media
        {
            get => m_media;
            set => m_media = value ?? string.Empty;
        }

        public string TextureName
        {
            get => m_textureName;
            set => m_textureName = value ?? string.Empty;
        }

        public AlphaSourceType? AlphaSource { get; set; }

        public Vector2? UVTranslation { get; set; }

        public Vector2? UVScaling { get; set; }

        public CropBase? Cropping { get; set; }

        public WrapMode? WrapModeU
        {
            get => InternalGetEnumType(nameof(WrapModeU), out WrapMode mode) ? mode : (WrapMode?)null;
            set => InternalSetEnumType(nameof(WrapModeU), value.HasValue, (int)(value ?? 0), "enum", string.Empty);
        }

        public WrapMode? WrapModeV
        {
            get => InternalGetEnumType(nameof(WrapModeV), out WrapMode mode) ? mode : (WrapMode?)null;
            set => InternalSetEnumType(nameof(WrapModeV), value.HasValue, (int)(value ?? 0), "enum", string.Empty);
        }

        public bool? UseMaterial
        {
            get => InternalGetPrimitive<bool>(nameof(UseMaterial), IElementPropertyType.Bool);
            set => InternalSetPrimitive(nameof(UseMaterial), IElementPropertyType.Bool, value, "bool", string.Empty);
        }

        public bool? UseMipMap
        {
            get => InternalGetPrimitive<bool>(nameof(UseMipMap), IElementPropertyType.Bool);
            set => InternalSetPrimitive(nameof(UseMipMap), IElementPropertyType.Bool, value, "bool", string.Empty);
        }

        public string UVSet
        {
            get => InternalGetReference<string>(nameof(UVSet), IElementPropertyType.String);
            set => InternalSetReference(nameof(UVSet), IElementPropertyType.String, value, "KString", string.Empty);
        }

        internal Texture(IElement element, IScene scene) : base(element, scene)
        {
            m_absolute = string.Empty;
            m_relative = string.Empty;
            m_media = string.Empty;
            m_textureName = string.Empty;

            if (element is null) return;

            var absolute = element.FindChild("FileName");

            if (!(absolute is null) && absolute.Attributes.Length > 0 &&
                absolute.Attributes[0].Type == IElementAttributeType.String)
                m_absolute = absolute.Attributes[0].GetElementValue().ToString() ?? string.Empty;

            var relative = element.FindChild("RelativeFilename");

            if (!(relative is null) && relative.Attributes.Length > 0 &&
                relative.Attributes[0].Type == IElementAttributeType.String)
                m_relative = relative.Attributes[0].GetElementValue().ToString() ?? string.Empty;

            var media = element.FindChild("Media");

            if (!(media is null) && media.Attributes.Length > 0 &&
                media.Attributes[0].Type == IElementAttributeType.String)
                m_media = media.Attributes[0].GetElementValue().ToString().Substring("::").Substring("\x00\x01")
                    .Trim('\x00') ?? string.Empty;

            var texture = element.FindChild("TextureName");

            if (!(texture is null) && texture.Attributes.Length > 0 &&
                texture.Attributes[0].Type == IElementAttributeType.String)
                m_textureName =
                    texture.Attributes[0].GetElementValue().ToString().Substring("::").Substring("\x00\x01")
                        .Trim('\x00') ?? string.Empty;

            var uvTranslation = element.FindChild("ModelUVTranslation");

            if (!(uvTranslation is null) && uvTranslation.Attributes.Length > 1)
                UVTranslation = new Vector2
                (
                    Convert.ToDouble(uvTranslation.Attributes[0].GetElementValue()),
                    Convert.ToDouble(uvTranslation.Attributes[1].GetElementValue())
                );

            var uvScaling = element.FindChild("ModelUVScaling");

            if (!(uvScaling is null) && uvScaling.Attributes.Length > 1)
                UVScaling = new Vector2
                (
                    Convert.ToDouble(uvScaling.Attributes[0].GetElementValue()),
                    Convert.ToDouble(uvScaling.Attributes[1].GetElementValue())
                );

            var alphaSource = element.FindChild("Texture_Alpha_Source");

            if (!(alphaSource is null) && alphaSource.Attributes.Length > 0 &&
                alphaSource.Attributes[0].Type == IElementAttributeType.String)
                if (Enum.TryParse(alphaSource.Attributes[0].GetElementValue().ToString(), out AlphaSourceType type))
                    AlphaSource = type;

            var cropping = element.FindChild("Cropping");

            if (!(cropping is null) && cropping.Attributes.Length > 3)
                Cropping = new CropBase
                {
                    V0 = Convert.ToInt32(cropping.Attributes[0].GetElementValue()),
                    V1 = Convert.ToInt32(cropping.Attributes[0].GetElementValue()),
                    V2 = Convert.ToInt32(cropping.Attributes[0].GetElementValue()),
                    V3 = Convert.ToInt32(cropping.Attributes[0].GetElementValue())
                };
        }

        private void InternalSetClip(Clip clip)
        {
            if (clip is null)
            {
                m_video = null;
                return;
            }

            if (clip.Scene != Scene) throw new Exception("Clip should share same scene with texture");

            m_video = clip;
        }

        public override Connection[] GetConnections()
        {
            if (m_video is null)
                return Array.Empty<Connection>();
            return new[]
            {
                new Connection(Connection.ConnectionType.Object, m_video.GetHashCode(), GetHashCode())
            };
        }

        public override void ResolveLink(FBXObject linker, IElementAttribute attribute)
        {
            if (linker.Class == FBXClassType.Video && linker.Type == FBXObjectType.Clip)
                InternalSetClip(linker as Clip);
        }

        public override IElement AsElement(bool binary)
        {
            var index = 0;
            var count = 7 +
                        (UVTranslation.HasValue ? 1 : 0) +
                        (UVScaling.HasValue ? 1 : 0) +
                        (AlphaSource.HasValue ? 1 : 0) +
                        (Cropping.HasValue ? 1 : 0);

            var textureName = string.IsNullOrWhiteSpace(m_textureName)
                ? string.Empty
                : m_textureName + (binary ? "\x00\x01" : "::") + "Texture";

            var mediaName = string.IsNullOrWhiteSpace(m_media)
                ? string.Empty
                : m_media + (binary ? "\x00\x01" : "::") + "Video";

            var elements = new IElement[count];

            elements[index++] =
                Element.WithAttribute("Type", ElementaryFactory.GetElementAttribute("TextureVideoClip"));
            elements[index++] = Element.WithAttribute("Version", ElementaryFactory.GetElementAttribute(202));
            elements[index++] =
                Element.WithAttribute("TextureName", ElementaryFactory.GetElementAttribute(textureName));
            elements[index++] = BuildProperties70();
            elements[index++] = Element.WithAttribute("Media", ElementaryFactory.GetElementAttribute(mediaName));
            elements[index++] = Element.WithAttribute("FileName", ElementaryFactory.GetElementAttribute(m_absolute));
            elements[index++] =
                Element.WithAttribute("RelativeFilename", ElementaryFactory.GetElementAttribute(m_relative));

            if (UVTranslation.HasValue)
                elements[index++] = new Element("ModelUVTranslation", null, new[]
                {
                    ElementaryFactory.GetElementAttribute(UVTranslation.Value.X),
                    ElementaryFactory.GetElementAttribute(UVTranslation.Value.Y)
                });

            if (UVScaling.HasValue)
                elements[index++] = new Element("ModelUVScaling", null, new[]
                {
                    ElementaryFactory.GetElementAttribute(UVScaling.Value.X),
                    ElementaryFactory.GetElementAttribute(UVScaling.Value.Y)
                });

            if (AlphaSource.HasValue)
                elements[index++] = new Element("Texture_Alpha_Source", null, new[]
                {
                    ElementaryFactory.GetElementAttribute(AlphaSource.ToString())
                });

            if (Cropping.HasValue)
                elements[index++] = new Element("Cropping", null, new[]
                {
                    ElementaryFactory.GetElementAttribute(Cropping.Value.V0),
                    ElementaryFactory.GetElementAttribute(Cropping.Value.V1),
                    ElementaryFactory.GetElementAttribute(Cropping.Value.V2),
                    ElementaryFactory.GetElementAttribute(Cropping.Value.V3)
                });

            return new Element(Class.ToString(), elements, BuildAttributes("Texture", string.Empty, binary));
        }
    }

    public class TextureBuilder : BuilderBase
    {
        private string m_absolute;
        private string m_relative;
        private string m_media;
        private string m_textureName;
        private Vector2? m_uvTraslation;
        private Vector2? m_uvScaling;
        private Texture.AlphaSourceType? m_alpha;
        private Texture.CropBase? m_cropping;

        public Clip Video { get; private set; }

        public TextureBuilder(Scene scene) : base(scene)
        {
        }

        public Texture BuildTexture()
        {
            var texture = m_scene.CreateTexture();

            texture.Name = m_name;
            texture.Media = m_media;
            texture.TextureName = m_textureName;

            if (Video is null)
            {
                texture.AbsolutePath = m_absolute;
                texture.RelativePath = m_relative;
            }
            else
            {
                texture.Data = Video;
                texture.AbsolutePath = Video.AbsolutePath;
                texture.RelativePath = Video.RelativePath;
                texture.UseMipMap = Video.UsesMipMaps;
            }

            texture.UVTranslation = m_uvTraslation;
            texture.UVScaling = m_uvScaling;
            texture.AlphaSource = m_alpha;
            texture.Cropping = m_cropping;

            foreach (var property in m_properties) texture.AddProperty(property);

            return texture;
        }

        public TextureBuilder WithName(string name)
        {
            SetObjectName(name);
            return this;
        }

        public TextureBuilder WithFBXProperty<T>(string name, T value, bool isUser = false)
        {
            SetFBXProperty(name, value, isUser);
            return this;
        }

        public TextureBuilder WithFBXProperty<T>(string name, T value, IElementPropertyFlags flags)
        {
            SetFBXProperty(name, value, flags);
            return this;
        }

        public TextureBuilder WithFBXProperty<T>(FBXProperty<T> property)
        {
            SetFBXProperty(property);
            return this;
        }

        public TextureBuilder WithVideo(Clip video)
        {
            if (video is null || video.Scene == m_scene)
            {
                Video = video;

                return this;
            }

            throw new ArgumentException("Video should share same scene as the texture");
        }

        public TextureBuilder WithAbsolutePath(string path)
        {
            if (Video is null) m_absolute = path;

            return this;
        }

        public TextureBuilder WithRelativePath(string path)
        {
            if (Video is null && !string.IsNullOrWhiteSpace(path)) m_relative = path;

            return this;
        }

        public TextureBuilder WithMedia(string media)
        {
            m_media = media;
            return this;
        }

        public TextureBuilder WithTextureName(string name)
        {
            m_textureName = name;
            return this;
        }

        public TextureBuilder WithUVTranslation(Vector2 uv)
        {
            m_uvTraslation = uv;
            return this;
        }

        public TextureBuilder WithUVScaling(Vector2 uv)
        {
            m_uvScaling = uv;
            return this;
        }

        public TextureBuilder WithAlphaSource(Texture.AlphaSourceType alphaSource)
        {
            m_alpha = alphaSource;
            return this;
        }

        public TextureBuilder WithCropping(Texture.CropBase cropping)
        {
            m_cropping = cropping;
            return this;
        }
    }
}