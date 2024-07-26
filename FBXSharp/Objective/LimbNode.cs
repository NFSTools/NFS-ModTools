using FBXSharp.Core;

namespace FBXSharp.Objective
{
    public class LimbNode : Model
    {
        public static readonly FBXObjectType FType = FBXObjectType.LimbNode;

        public override FBXObjectType Type => FType;

        public override bool SupportsAttribute => true;

        internal LimbNode(IElement element, IScene scene) : base(element, scene)
        {
        }

        public override IElement AsElement(bool binary)
        {
            return MakeElement("Model", binary);
        }
    }

    public class LimbNodeAttribute : NodeAttribute
    {
        private string m_typeFlags;

        public static readonly FBXObjectType FType = FBXObjectType.LimbNode;

        public override FBXObjectType Type => FType;

        public string TypeFlags
        {
            get => m_typeFlags;
            set => m_typeFlags = value ?? string.Empty;
        }

        internal LimbNodeAttribute(IElement element, IScene scene) : base(element, scene)
        {
            if (element is null) return;

            var typeFlags = element.FindChild("TypeFlags");

            if (typeFlags is null || typeFlags.Attributes.Length == 0 ||
                typeFlags.Attributes[0].Type != IElementAttributeType.String)
                m_typeFlags = string.Empty;
            else
                m_typeFlags = typeFlags.Attributes[0].GetElementValue().ToString();
        }

        public override IElement AsElement(bool binary)
        {
            var hasAnyProperties = Properties.Count != 0;

            var elements = new IElement[1 + (hasAnyProperties ? 1 : 0)];

            elements[0] = Element.WithAttribute("TypeFlags", ElementaryFactory.GetElementAttribute(m_typeFlags));

            if (hasAnyProperties) elements[1] = BuildProperties70();

            return new Element(Class.ToString(), elements, BuildAttributes("NodeAttribute", Type.ToString(), binary));
        }
    }
}