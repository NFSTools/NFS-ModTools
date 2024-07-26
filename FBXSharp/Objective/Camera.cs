using System;
using FBXSharp.Core;
using FBXSharp.ValueTypes;

namespace FBXSharp.Objective
{
    public class Camera : Model
    {
        public static readonly FBXObjectType FType = FBXObjectType.Camera;

        public override FBXObjectType Type => FType;

        public override bool SupportsAttribute => true;

        internal Camera(IElement element, IScene scene) : base(element, scene)
        {
        }

        public override IElement AsElement(bool binary)
        {
            return MakeElement("Model", binary);
        }
    }

    public class CameraAttribute : NodeAttribute
    {
        public static readonly FBXObjectType FType = FBXObjectType.Camera;

        public override FBXObjectType Type => FType;

        public Vector3 Position { get; set; }

        public Vector3 Up { get; set; }

        public Vector3 LookAt { get; set; }

        public bool ShowInfoOnMoving { get; set; }

        public bool ShowAudio { get; set; }

        public ColorRGB AudioColor { get; set; }

        public double CameraOrthoZoom { get; set; }

        internal CameraAttribute(IElement element, IScene scene) : base(element, scene)
        {
            if (element is null) return;

            var position = element.FindChild(nameof(Position));
            var up = element.FindChild(nameof(Up));
            var lookAt = element.FindChild(nameof(LookAt));
            var showInfo = element.FindChild(nameof(ShowInfoOnMoving));
            var showAudio = element.FindChild(nameof(ShowAudio));
            var audioColor = element.FindChild(nameof(AudioColor));
            var orthoZoom = element.FindChild(nameof(CameraOrthoZoom));

            if (!(position is null) && position.Attributes.Length > 2)
                Position = new Vector3
                {
                    X = Convert.ToDouble(position.Attributes[0].GetElementValue()),
                    Y = Convert.ToDouble(position.Attributes[1].GetElementValue()),
                    Z = Convert.ToDouble(position.Attributes[2].GetElementValue())
                };

            if (!(up is null) && up.Attributes.Length > 2)
                Up = new Vector3
                {
                    X = Convert.ToDouble(up.Attributes[0].GetElementValue()),
                    Y = Convert.ToDouble(up.Attributes[1].GetElementValue()),
                    Z = Convert.ToDouble(up.Attributes[2].GetElementValue())
                };

            if (!(lookAt is null) && lookAt.Attributes.Length > 2)
                LookAt = new Vector3
                {
                    X = Convert.ToDouble(lookAt.Attributes[0].GetElementValue()),
                    Y = Convert.ToDouble(lookAt.Attributes[1].GetElementValue()),
                    Z = Convert.ToDouble(lookAt.Attributes[2].GetElementValue())
                };

            if (!(showInfo is null) && showInfo.Attributes.Length > 0)
                ShowInfoOnMoving = Convert.ToBoolean(showInfo.Attributes[0].GetElementValue());
            else
                ShowInfoOnMoving = true;

            if (!(showAudio is null) && showAudio.Attributes.Length > 0)
                ShowAudio = Convert.ToBoolean(showAudio.Attributes[0].GetElementValue());
            else
                ShowAudio = false;

            if (!(audioColor is null) && audioColor.Attributes.Length > 2)
                AudioColor = new ColorRGB
                {
                    R = Convert.ToDouble(audioColor.Attributes[0].GetElementValue()),
                    G = Convert.ToDouble(audioColor.Attributes[1].GetElementValue()),
                    B = Convert.ToDouble(audioColor.Attributes[2].GetElementValue())
                };
            else
                AudioColor = new ColorRGB(0.0, 1.0, 0.0);

            if (!(orthoZoom is null) && orthoZoom.Attributes.Length > 0)
                CameraOrthoZoom = Convert.ToDouble(orthoZoom.Attributes[0].GetElementValue());
            else
                CameraOrthoZoom = 1.0;
        }

        public override IElement AsElement(bool binary)
        {
            var elements = new IElement[10];

            elements[0] = BuildProperties70();
            elements[1] = Element.WithAttribute("TypeFlags", ElementaryFactory.GetElementAttribute("Camera"));
            elements[2] = Element.WithAttribute("GeometryVersion", ElementaryFactory.GetElementAttribute(124));

            elements[3] = new Element(nameof(Position), null, new[]
            {
                ElementaryFactory.GetElementAttribute(Position.X),
                ElementaryFactory.GetElementAttribute(Position.Y),
                ElementaryFactory.GetElementAttribute(Position.Z)
            });

            elements[4] = new Element(nameof(Up), null, new[]
            {
                ElementaryFactory.GetElementAttribute(Up.X),
                ElementaryFactory.GetElementAttribute(Up.Y),
                ElementaryFactory.GetElementAttribute(Up.Z)
            });

            elements[5] = new Element(nameof(LookAt), null, new[]
            {
                ElementaryFactory.GetElementAttribute(LookAt.X),
                ElementaryFactory.GetElementAttribute(LookAt.Y),
                ElementaryFactory.GetElementAttribute(LookAt.Z)
            });

            elements[8] = new Element(nameof(AudioColor), null, new[]
            {
                ElementaryFactory.GetElementAttribute(AudioColor.R),
                ElementaryFactory.GetElementAttribute(AudioColor.G),
                ElementaryFactory.GetElementAttribute(AudioColor.B)
            });

            elements[6] = Element.WithAttribute(nameof(ShowInfoOnMoving),
                ElementaryFactory.GetElementAttribute(ShowInfoOnMoving));
            elements[7] = Element.WithAttribute(nameof(ShowAudio), ElementaryFactory.GetElementAttribute(ShowAudio));
            elements[9] = Element.WithAttribute(nameof(CameraOrthoZoom),
                ElementaryFactory.GetElementAttribute(CameraOrthoZoom));

            return new Element(Class.ToString(), elements, BuildAttributes("NodeAttribute", Type.ToString(), binary));
        }
    }
}