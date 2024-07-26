using FBXSharp.Core;

namespace FBXSharp.Objective
{
    public class Light : Model
    {
        public static readonly FBXObjectType FType = FBXObjectType.Light;

        public override FBXObjectType Type => FType;

        public override bool SupportsAttribute => true;

        internal Light(IElement element, IScene scene) : base(element, scene)
        {
        }

        public override IElement AsElement(bool binary)
        {
            return MakeElement("Model", binary);
        }
    }

    public class LightAttribute : NodeAttribute
    {
        public static readonly FBXObjectType FType = FBXObjectType.Light;

        public override FBXObjectType Type => FType;

        internal LightAttribute(IElement element, IScene scene) : base(element, scene)
        {
        }

        public override IElement AsElement(bool binary)
        {
            var elements = new IElement[3];

            elements[0] = Element.WithAttribute("TypeFlags", ElementaryFactory.GetElementAttribute("Light"));
            elements[1] = Element.WithAttribute("GeometryVersion", ElementaryFactory.GetElementAttribute(124));
            elements[2] = BuildProperties70();

            return new Element(Class.ToString(), elements, BuildAttributes("NodeAttribute", Type.ToString(), binary));
        }
    }
}