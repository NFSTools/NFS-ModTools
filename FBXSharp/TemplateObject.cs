using System;
using FBXSharp.Core;

namespace FBXSharp
{
    public enum TemplateCreationType
    {
        DontCreateIfDuplicated,
        MergeIfExistingIsFound,
        NewOverrideAnyExisting
    }

    public class TemplateObject : FBXObject
    {
        public static readonly FBXObjectType FType = FBXObjectType.Template;

        public static readonly FBXClassType FClass = FBXClassType.Template;

        public override FBXObjectType Type => FType;

        public override FBXClassType Class => FClass;

        public FBXClassType OverridableType { get; }

        public TemplateObject(FBXClassType type, IElement element, IScene scene) : base(null, scene)
        {
            OverridableType = type;

            Initialize(element);
        }

        public void Initialize(IElement element)
        {
            if (element is null) return;

            RemoveAllProperties();

            if (element.Attributes.Length > 0 && element.Attributes[0].Type == IElementAttributeType.String)
                Name = element.Attributes[0].GetElementValue().ToString();

            ParseProperties70(element);
        }

        public void MergeWith(TemplateObject template)
        {
            if (template is null) throw new ArgumentNullException("Merger template passed cannot be null");

            if (template.OverridableType != OverridableType)
                throw new ArgumentException(
                    $"Cannot merge template of type {OverridableType} with template of type {template.OverridableType}");

            foreach (var property in template.Properties) AddProperty(property);
        }

        public override IElement AsElement(bool binary)
        {
            throw new NotSupportedException("Templates cannot be serialized");
        }
    }
}