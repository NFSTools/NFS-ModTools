using System;
using FBXSharp.Core;

namespace FBXSharp.Objective
{
    public class Clip : FBXObject
    {
        public enum ImageType
        {
            PNG,
            JPG,
            DDS,
            WEBP,
            KTX2,
            Other
        }

        public static readonly FBXObjectType FType = FBXObjectType.Clip;

        public static readonly FBXClassType FClass = FBXClassType.Video;

        public override FBXObjectType Type => FType;

        public override FBXClassType Class => FClass;

        public ImageType Image { get; private set; }

        public byte[] Content { get; private set; }

        public string AbsolutePath { get; private set; }

        public string RelativePath { get; private set; }

        public bool UsesMipMaps { get; private set; }

        public string Path
        {
            get => InternalGetReference<string>(nameof(Path), IElementPropertyType.String);
            set => InternalSetReference(nameof(Path), IElementPropertyType.String, value, "KString", "XRefUrl");
        }

        public string RelPath
        {
            get => InternalGetReference<string>(nameof(RelPath), IElementPropertyType.String);
            set => InternalSetReference(nameof(RelPath), IElementPropertyType.String, value, "KString", "XRefUrl");
        }

        internal Clip(IElement element, IScene scene) : base(element, scene)
        {
            Image = ImageType.Other;
            AbsolutePath = string.Empty;
            RelativePath = string.Empty;
            Content = Array.Empty<byte>();

            if (element is null) return;

            var content = element.FindChild("Content");

            if (!(content is null) && content.Attributes.Length > 0 &&
                content.Attributes[0].Type == IElementAttributeType.Binary)
            {
                Content = content.Attributes[0].GetElementValue() as byte[];
                Image = IsImage(Content);
            }

            var absolute = element.FindChild("Filename");

            if (!(absolute is null) && absolute.Attributes.Length > 0 &&
                absolute.Attributes[0].Type == IElementAttributeType.String)
                AbsolutePath = absolute.Attributes[0].GetElementValue().ToString() ?? string.Empty;

            var relative = element.FindChild("RelativeFilename");

            if (!(relative is null) && relative.Attributes.Length > 0 &&
                relative.Attributes[0].Type == IElementAttributeType.String)
                RelativePath = relative.Attributes[0].GetElementValue().ToString() ?? string.Empty;

            var useMipMap = element.FindChild("UseMipMap");

            if (!(useMipMap is null) && useMipMap.Attributes.Length > 0)
                UsesMipMaps = Convert.ToBoolean(useMipMap.Attributes[0].GetElementValue());
            else
                UsesMipMaps = Image == ImageType.DDS;
        }

        public void SetAbsolutePath(string path)
        {
            AbsolutePath = path;
        }

        public void SetRelativePath(string path)
        {
            RelativePath = path;
        }

        public void SetContent(byte[] content)
        {
            Content = content ?? Array.Empty<byte>();
            Image = IsImage(Content);
            UsesMipMaps = Image == ImageType.DDS;
        }

        public override IElement AsElement(bool binary)
        {
            var elements = new IElement[5 + (Content.Length == 0 ? 0 : 1)];

            elements[0] = Element.WithAttribute("Type", ElementaryFactory.GetElementAttribute("Clip"));
            elements[1] = BuildProperties70();
            elements[2] =
                Element.WithAttribute("UseMipMap", ElementaryFactory.GetElementAttribute(UsesMipMaps ? 1 : 0));
            elements[3] = Element.WithAttribute("Filename", ElementaryFactory.GetElementAttribute(AbsolutePath));
            elements[4] =
                Element.WithAttribute("RelativeFilename", ElementaryFactory.GetElementAttribute(RelativePath));

            if (Content.Length != 0)
                elements[5] = Element.WithAttribute("Content", ElementaryFactory.GetElementAttribute(Content));

            return new Element(Class.ToString(), elements, BuildAttributes("Video", Type.ToString(), binary));
        }

        private static bool IsPngImage(byte[] data)
        {
            return data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47;
        }

        private static bool IsJpgImage(byte[] data)
        {
            return data[0] == 0xFF && data[1] == 0xD8;
        }

        private static bool IsDdsImage(byte[] data)
        {
            return data[0] == 0x44 && data[1] == 0x44 && data[2] == 0x53 && data[3] == 0x20;
        }

        private static bool IsWebpImage(byte[] data)
        {
            return data[0] == 0x52 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x46 &&
                   data[8] == 0x57 && data[9] == 0x45 && data[10] == 0x42 && data[11] == 0x50;
        }

        private static bool IsKtx2Image(byte[] data)
        {
            return data[0] == 0xBB && data[1] == 0x30 && data[2] == 0x32 && data[3] == 0x20 &&
                   data[5] == 0x58 && data[6] == 0x54 && data[7] == 0x4B && data[7] == 0xAB &&
                   data[8] == 0x0A && data[9] == 0x1A && data[10] == 0x0A && data[11] == 0x0D;
        }

        private static ImageType IsImage(byte[] data)
        {
            if (data is null || data.Length < 12) return ImageType.Other;

            if (IsDdsImage(data)) return ImageType.DDS;

            if (IsJpgImage(data)) return ImageType.JPG;

            if (IsPngImage(data)) return ImageType.PNG;

            if (IsWebpImage(data)) return ImageType.WEBP;

            if (IsKtx2Image(data)) return ImageType.KTX2;

            return ImageType.Other;
        }
    }
}