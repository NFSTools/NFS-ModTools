using FBXSharp.Core;
using FBXSharp.Objective;
using FBXSharp.ValueTypes;
using IEPF = FBXSharp.Core.IElementPropertyFlags;

namespace FBXSharp
{
    public static class TemplateFactory
    {
        public static TemplateObject GetAnimationStackTemplate(IScene scene = null)
        {
            var template = new TemplateObject(FBXClassType.AnimationStack, null, scene)
            {
                Name = "FbxAnimStack"
            };

            template.AddProperty(new FBXProperty<string>("KString", string.Empty, "Description", IEPF.Imported,
                string.Empty));
            template.AddProperty(new FBXProperty<TimeBase>("KTime", "Time", "LocalStart", IEPF.Imported,
                new TimeBase(0.0)));
            template.AddProperty(new FBXProperty<TimeBase>("KTime", "Time", "LocalStop", IEPF.Imported,
                new TimeBase(0.0)));
            template.AddProperty(new FBXProperty<TimeBase>("KTime", "Time", "ReferenceStart", IEPF.Imported,
                new TimeBase(0.0)));
            template.AddProperty(new FBXProperty<TimeBase>("KTime", "Time", "ReferenceStop", IEPF.Imported,
                new TimeBase(0.0)));

            return template;
        }

        public static TemplateObject GetAnimationLayerTemplate(IScene scene = null)
        {
            var template = new TemplateObject(FBXClassType.AnimationLayer, null, scene)
            {
                Name = "FbxAnimLayer"
            };

            template.AddProperty(new FBXProperty<double>("Number", string.Empty, "Weight",
                IEPF.Imported | IEPF.Animatable, 100.0));
            template.AddProperty(new FBXProperty<bool>("bool", string.Empty, "Mute", IEPF.Imported, false));
            template.AddProperty(new FBXProperty<bool>("bool", string.Empty, "Solo", IEPF.Imported, false));
            template.AddProperty(new FBXProperty<bool>("bool", string.Empty, "Lock", IEPF.Imported, false));
            template.AddProperty(new FBXProperty<Vector3>("ColorRGB", "Color", "Color", IEPF.Imported,
                new Vector3(0.8, 0.8, 0.8)));
            template.AddProperty(new FBXProperty<Enumeration>("enum", string.Empty, "BlendMode", IEPF.Imported,
                new Enumeration()));
            template.AddProperty(new FBXProperty<Enumeration>("enum", string.Empty, "RotationAccumulationMode",
                IEPF.Imported, new Enumeration()));
            template.AddProperty(new FBXProperty<Enumeration>("enum", string.Empty, "ScaleAccumulationMode",
                IEPF.Imported, new Enumeration()));
            template.AddProperty(new FBXProperty<ulong>("ULongLong", string.Empty, "BlendModeBypass", IEPF.Imported,
                0));

            return template;
        }

        public static TemplateObject GetModelTemplate(IScene scene = null)
        {
            var template = new TemplateObject(FBXClassType.Model, null, scene)
            {
                Name = "FbxNode"
            };

            template.AddProperty(new FBXProperty<Enumeration>("enum", string.Empty, "QuaternionInterpolate",
                IEPF.Imported, new Enumeration()));
            template.AddProperty(new FBXProperty<Vector3>("Vector3D", "Vector", "RotationOffset", IEPF.Imported,
                new Vector3(0.0, 0.0, 0.0)));
            template.AddProperty(new FBXProperty<Vector3>("Vector3D", "Vector", "RotationPivot", IEPF.Imported,
                new Vector3(0.0, 0.0, 0.0)));
            template.AddProperty(new FBXProperty<Vector3>("Vector3D", "Vector", "ScalingOffset", IEPF.Imported,
                new Vector3(0.0, 0.0, 0.0)));
            template.AddProperty(new FBXProperty<Vector3>("Vector3D", "Vector", "ScalingPivot", IEPF.Imported,
                new Vector3(0.0, 0.0, 0.0)));
            template.AddProperty(new FBXProperty<bool>("bool", string.Empty, "TranslationActive", IEPF.Imported,
                false));
            template.AddProperty(new FBXProperty<Vector3>("Vector3D", "Vector", "TranslationMin", IEPF.Imported,
                new Vector3(0.0, 0.0, 0.0)));
            template.AddProperty(new FBXProperty<Vector3>("Vector3D", "Vector", "TranslationMax", IEPF.Imported,
                new Vector3(0.0, 0.0, 0.0)));
            template.AddProperty(new FBXProperty<bool>("bool", string.Empty, "TranslationMinX", IEPF.Imported, false));
            template.AddProperty(new FBXProperty<bool>("bool", string.Empty, "TranslationMinY", IEPF.Imported, false));
            template.AddProperty(new FBXProperty<bool>("bool", string.Empty, "TranslationMinZ", IEPF.Imported, false));
            template.AddProperty(new FBXProperty<bool>("bool", string.Empty, "TranslationMaxX", IEPF.Imported, false));
            template.AddProperty(new FBXProperty<bool>("bool", string.Empty, "TranslationMaxY", IEPF.Imported, false));
            template.AddProperty(new FBXProperty<bool>("bool", string.Empty, "TranslationMaxZ", IEPF.Imported, false));
            template.AddProperty(new FBXProperty<Enumeration>("enum", string.Empty, "RotationOrder", IEPF.Imported,
                new Enumeration()));
            template.AddProperty(new FBXProperty<bool>("bool", string.Empty, "RotationSpaceForLimitOnly", IEPF.Imported,
                false));
            template.AddProperty(new FBXProperty<double>("double", "Number", "RotationStiffnessX", IEPF.Imported, 0.0));
            template.AddProperty(new FBXProperty<double>("double", "Number", "RotationStiffnessY", IEPF.Imported, 0.0));
            template.AddProperty(new FBXProperty<double>("double", "Number", "RotationStiffnessZ", IEPF.Imported, 0.0));
            template.AddProperty(new FBXProperty<double>("double", "Number", "AxisLen", IEPF.Imported, 10.0));
            template.AddProperty(new FBXProperty<Vector3>("Vector3D", "Vector", "PreRotation", IEPF.Imported,
                new Vector3(0.0, 0.0, 0.0)));
            template.AddProperty(new FBXProperty<Vector3>("Vector3D", "Vector", "PostRotation", IEPF.Imported,
                new Vector3(0.0, 0.0, 0.0)));
            template.AddProperty(new FBXProperty<bool>("bool", string.Empty, "RotationActive", IEPF.Imported, false));
            template.AddProperty(new FBXProperty<Vector3>("Vector3D", "Vector", "RotationMin", IEPF.Imported,
                new Vector3(0.0, 0.0, 0.0)));
            template.AddProperty(new FBXProperty<Vector3>("Vector3D", "Vector", "RotationMax", IEPF.Imported,
                new Vector3(0.0, 0.0, 0.0)));
            template.AddProperty(new FBXProperty<bool>("bool", string.Empty, "RotationMinX", IEPF.Imported, false));
            template.AddProperty(new FBXProperty<bool>("bool", string.Empty, "RotationMinY", IEPF.Imported, false));
            template.AddProperty(new FBXProperty<bool>("bool", string.Empty, "RotationMinZ", IEPF.Imported, false));
            template.AddProperty(new FBXProperty<bool>("bool", string.Empty, "RotationMaxX", IEPF.Imported, false));
            template.AddProperty(new FBXProperty<bool>("bool", string.Empty, "RotationMaxY", IEPF.Imported, false));
            template.AddProperty(new FBXProperty<bool>("bool", string.Empty, "RotationMaxZ", IEPF.Imported, false));
            template.AddProperty(new FBXProperty<Enumeration>("enum", string.Empty, "InheritType", IEPF.Imported,
                new Enumeration()));
            template.AddProperty(new FBXProperty<bool>("bool", string.Empty, "ScalingActive", IEPF.Imported, false));
            template.AddProperty(new FBXProperty<Vector3>("Vector3D", "Vector", "ScalingMin", IEPF.Imported,
                new Vector3(0.0, 0.0, 0.0)));
            template.AddProperty(new FBXProperty<Vector3>("Vector3D", "Vector", "ScalingMax", IEPF.Imported,
                new Vector3(1.0, 1.0, 1.0)));
            template.AddProperty(new FBXProperty<bool>("bool", string.Empty, "ScalingMinX", IEPF.Imported, false));
            template.AddProperty(new FBXProperty<bool>("bool", string.Empty, "ScalingMinY", IEPF.Imported, false));
            template.AddProperty(new FBXProperty<bool>("bool", string.Empty, "ScalingMinZ", IEPF.Imported, false));
            template.AddProperty(new FBXProperty<bool>("bool", string.Empty, "ScalingMaxX", IEPF.Imported, false));
            template.AddProperty(new FBXProperty<bool>("bool", string.Empty, "ScalingMaxY", IEPF.Imported, false));
            template.AddProperty(new FBXProperty<bool>("bool", string.Empty, "ScalingMaxZ", IEPF.Imported, false));
            template.AddProperty(new FBXProperty<Vector3>("Vector3D", "Vector", "GeometricTranslation", IEPF.Imported,
                new Vector3(0.0, 0.0, 0.0)));
            template.AddProperty(new FBXProperty<Vector3>("Vector3D", "Vector", "GeometricRotation", IEPF.Imported,
                new Vector3(0.0, 0.0, 0.0)));
            template.AddProperty(new FBXProperty<Vector3>("Vector3D", "Vector", "GeometricScaling", IEPF.Imported,
                new Vector3(1.0, 1.0, 1.0)));
            template.AddProperty(new FBXProperty<double>("double", "Number", "MinDampRangeX", IEPF.Imported, 0.0));
            template.AddProperty(new FBXProperty<double>("double", "Number", "MinDampRangeY", IEPF.Imported, 0.0));
            template.AddProperty(new FBXProperty<double>("double", "Number", "MinDampRangeZ", IEPF.Imported, 0.0));
            template.AddProperty(new FBXProperty<double>("double", "Number", "MaxDampRangeX", IEPF.Imported, 0.0));
            template.AddProperty(new FBXProperty<double>("double", "Number", "MaxDampRangeY", IEPF.Imported, 0.0));
            template.AddProperty(new FBXProperty<double>("double", "Number", "MaxDampRangeZ", IEPF.Imported, 0.0));
            template.AddProperty(new FBXProperty<double>("double", "Number", "MinDampStrengthX", IEPF.Imported, 0.0));
            template.AddProperty(new FBXProperty<double>("double", "Number", "MinDampStrengthY", IEPF.Imported, 0.0));
            template.AddProperty(new FBXProperty<double>("double", "Number", "MinDampStrengthZ", IEPF.Imported, 0.0));
            template.AddProperty(new FBXProperty<double>("double", "Number", "MaxDampStrengthX", IEPF.Imported, 0.0));
            template.AddProperty(new FBXProperty<double>("double", "Number", "MaxDampStrengthY", IEPF.Imported, 0.0));
            template.AddProperty(new FBXProperty<double>("double", "Number", "MaxDampStrengthZ", IEPF.Imported, 0.0));
            template.AddProperty(new FBXProperty<double>("double", "Number", "PreferedAngleX", IEPF.Imported, 0.0));
            template.AddProperty(new FBXProperty<double>("double", "Number", "PreferedAngleY", IEPF.Imported, 0.0));
            template.AddProperty(new FBXProperty<double>("double", "Number", "PreferedAngleZ", IEPF.Imported, 0.0));
            template.AddProperty(new FBXProperty<object>("object", string.Empty, "LookAtProperty", IEPF.Imported,
                null));
            template.AddProperty(new FBXProperty<object>("object", string.Empty, "UpVectorProperty", IEPF.Imported,
                null));
            template.AddProperty(new FBXProperty<bool>("bool", string.Empty, "Show", IEPF.Imported, true));
            template.AddProperty(new FBXProperty<bool>("bool", string.Empty, "NegativePercentShapeSupport",
                IEPF.Imported, true));
            template.AddProperty(new FBXProperty<int>("int", "Integer", "DefaultAttributeIndex", IEPF.Imported, -1));
            template.AddProperty(new FBXProperty<bool>("bool", string.Empty, "Freeze", IEPF.Imported, false));
            template.AddProperty(new FBXProperty<bool>("bool", string.Empty, "LODBox", IEPF.Imported, false));
            template.AddProperty(new FBXProperty<Vector3>("Lcl Translation", string.Empty, "Lcl Translation",
                IEPF.Imported | IEPF.Animatable, new Vector3(0.0, 0.0, 0.0)));
            template.AddProperty(new FBXProperty<Vector3>("Lcl Rotation", string.Empty, "Lcl Rotation",
                IEPF.Imported | IEPF.Animatable, new Vector3(0.0, 0.0, 0.0)));
            template.AddProperty(new FBXProperty<Vector3>("Lcl Scaling", string.Empty, "Lcl Scaling",
                IEPF.Imported | IEPF.Animatable, new Vector3(1.0, 1.0, 1.0)));
            template.AddProperty(new FBXProperty<double>("Visibility", string.Empty, "Visibility",
                IEPF.Imported | IEPF.Animatable, 1.0));
            template.AddProperty(new FBXProperty<bool>("Visibility Inheritance", string.Empty, "Visibility Inheritance",
                IEPF.Imported, true));

            return template;
        }

        public static TemplateObject GetNodeAttributeTemplate(IScene scene = null)
        {
            var template = new TemplateObject(FBXClassType.NodeAttribute, null, scene)
            {
                Name = "FbxNull"
            };

            template.AddProperty(new FBXProperty<Vector3>("ColorRGB", "Color", "Color", IEPF.Imported,
                new Vector3(0.8, 0.8, 0.8)));
            template.AddProperty(new FBXProperty<double>("double", "Number", "Size", IEPF.Imported, 100.0));
            template.AddProperty(new FBXProperty<Enumeration>("enum", string.Empty, "Look", IEPF.Imported,
                new Enumeration(1)));

            return template;
        }

        public static TemplateObject GetGeometryTemplate(IScene scene = null)
        {
            var template = new TemplateObject(FBXClassType.Geometry, null, scene)
            {
                Name = "FbxMesh"
            };

            template.AddProperty(new FBXProperty<Vector3>("ColorRGB", "Color", "Color", IEPF.Imported,
                new Vector3(0.8, 0.8, 0.8)));
            template.AddProperty(new FBXProperty<Vector3>("Vector3D", "Vector", "BBoxMin", IEPF.Imported,
                new Vector3(0.0, 0.0, 0.0)));
            template.AddProperty(new FBXProperty<Vector3>("Vector3D", "Vector", "BBoxMax", IEPF.Imported,
                new Vector3(0.0, 0.0, 0.0)));
            template.AddProperty(new FBXProperty<bool>("bool", string.Empty, "Primary Visibility", IEPF.Imported,
                true));
            template.AddProperty(new FBXProperty<bool>("bool", string.Empty, "Casts Shadows", IEPF.Imported, true));
            template.AddProperty(new FBXProperty<bool>("bool", string.Empty, "Receive Shadows", IEPF.Imported, true));

            return template;
        }

        public static TemplateObject GetMaterialTemplate(string shading = "", IScene scene = null)
        {
            switch (shading)
            {
                // #TODO

                case "":
                case "phong":
                case "Phong":
                default:
                    return GetSurfacePhongMaterialTemplate(scene);
            }
        }

        public static TemplateObject GetTextureTemplate(IScene scene = null)
        {
            var template = new TemplateObject(FBXClassType.Texture, null, scene)
            {
                Name = "FbxFileTexture"
            };

            template.AddProperty(new FBXProperty<Enumeration>("enum", string.Empty, "TextureTypeUse", IEPF.Imported,
                new Enumeration()));
            template.AddProperty(new FBXProperty<Enumeration>("enum", string.Empty, "AlphaSource", IEPF.Imported,
                new Enumeration(2)));
            template.AddProperty(new FBXProperty<double>("Number", string.Empty, "Texture alpha",
                IEPF.Imported | IEPF.Animatable, 1.0));
            template.AddProperty(new FBXProperty<Enumeration>("enum", string.Empty, "CurrentMappingType", IEPF.Imported,
                new Enumeration()));
            template.AddProperty(new FBXProperty<Enumeration>("enum", string.Empty, "WrapModeU", IEPF.Imported,
                new Enumeration()));
            template.AddProperty(new FBXProperty<Enumeration>("enum", string.Empty, "WrapModeV", IEPF.Imported,
                new Enumeration()));
            template.AddProperty(new FBXProperty<bool>("bool", string.Empty, "UVSwap", IEPF.Imported, false));
            template.AddProperty(new FBXProperty<bool>("bool", string.Empty, "PremultiplyAlpha", IEPF.Imported, true));
            template.AddProperty(new FBXProperty<Vector3>("Vector", string.Empty, "Translation",
                IEPF.Imported | IEPF.Animatable, new Vector3(0.0, 0.0, 0.0)));
            template.AddProperty(new FBXProperty<Vector3>("Vector", string.Empty, "Rotation",
                IEPF.Imported | IEPF.Animatable, new Vector3(0.0, 0.0, 0.0)));
            template.AddProperty(new FBXProperty<Vector3>("Vector", string.Empty, "Scaling",
                IEPF.Imported | IEPF.Animatable, new Vector3(1.0, 1.0, 1.0)));
            template.AddProperty(new FBXProperty<Vector3>("Vector3D", "Vector", "TextureRotationPivot", IEPF.Imported,
                new Vector3(0.0, 0.0, 0.0)));
            template.AddProperty(new FBXProperty<Vector3>("Vector3D", "Vector", "TextureScalingPivot", IEPF.Imported,
                new Vector3(0.0, 0.0, 0.0)));
            template.AddProperty(new FBXProperty<Enumeration>("enum", string.Empty, "CurrentTextureBlendMode",
                IEPF.Imported, new Enumeration(1)));
            template.AddProperty(new FBXProperty<string>("KString", string.Empty, "UVSet", IEPF.Imported, "default"));
            template.AddProperty(new FBXProperty<bool>("bool", string.Empty, "UseMaterial", IEPF.Imported, false));
            template.AddProperty(new FBXProperty<bool>("bool", string.Empty, "UseMipMap", IEPF.Imported, false));

            return template;
        }

        public static TemplateObject GetVideoTemplate(IScene scene = null)
        {
            var template = new TemplateObject(FBXClassType.Video, null, scene)
            {
                Name = "FbxVideo"
            };

            template.AddProperty(new FBXProperty<string>("KString", "XRefUrl", "Path", IEPF.Imported, string.Empty));
            template.AddProperty(new FBXProperty<string>("KString", "XRefUrl", "RelPath", IEPF.Imported, string.Empty));
            template.AddProperty(new FBXProperty<Vector3>("ColorRGB", "Color", "Color", IEPF.Imported,
                new Vector3(0.8, 0.8, 0.8)));
            template.AddProperty(new FBXProperty<TimeBase>("KTime", "Time", "ClipIn", IEPF.Imported,
                new TimeBase(0.0)));
            template.AddProperty(
                new FBXProperty<TimeBase>("KTime", "Time", "ClipOut", IEPF.Imported, new TimeBase(0.0)));
            template.AddProperty(new FBXProperty<TimeBase>("KTime", "Time", "Offset", IEPF.Imported,
                new TimeBase(0.0)));
            template.AddProperty(new FBXProperty<double>("double", "Number", "PlaySpeed", IEPF.Imported, 0.0));
            template.AddProperty(new FBXProperty<bool>("bool", string.Empty, "FreeRunning", IEPF.Imported, false));
            template.AddProperty(new FBXProperty<bool>("bool", string.Empty, "Loop", IEPF.Imported, false));
            template.AddProperty(new FBXProperty<bool>("bool", string.Empty, "Mute", IEPF.Imported, false));
            template.AddProperty(new FBXProperty<Enumeration>("enum", string.Empty, "AccessMode", IEPF.Imported,
                new Enumeration()));
            template.AddProperty(new FBXProperty<bool>("bool", string.Empty, "ImageSequence", IEPF.Imported, false));
            template.AddProperty(new FBXProperty<int>("int", "Integer", "ImageSequenceOffset", IEPF.Imported, 0));
            template.AddProperty(new FBXProperty<double>("double", "Number", "FrameRate", IEPF.Imported, 0.0));
            template.AddProperty(new FBXProperty<int>("int", "Integer", "LastFrame", IEPF.Imported, 0));
            template.AddProperty(new FBXProperty<int>("int", "Integer", "Width", IEPF.Imported, 0));
            template.AddProperty(new FBXProperty<int>("int", "Integer", "Height", IEPF.Imported, 0));
            template.AddProperty(new FBXProperty<int>("int", "Integer", "StartFrame", IEPF.Imported, 0));
            template.AddProperty(new FBXProperty<int>("int", "Integer", "StopFrame", IEPF.Imported, 0));
            template.AddProperty(new FBXProperty<Enumeration>("enum", string.Empty, "InterlaceMode", IEPF.Imported,
                new Enumeration()));

            return template;
        }

        public static TemplateObject GetSurfacePhongMaterialTemplate(IScene scene = null)
        {
            var template = new TemplateObject(FBXClassType.Material, null, scene)
            {
                Name = "FbxSurfacePhong"
            };

            template.AddProperty(new FBXProperty<string>("KString", string.Empty, "ShadingModel", IEPF.Imported,
                "Phong"));
            template.AddProperty(new FBXProperty<bool>("bool", string.Empty, "MultiLayer", IEPF.Imported, false));
            template.AddProperty(new FBXProperty<Vector3>("Color", string.Empty, "EmissiveColor",
                IEPF.Imported | IEPF.Animatable, new Vector3(0.0, 0.0, 0.0)));
            template.AddProperty(new FBXProperty<double>("Number", string.Empty, "EmissiveFactor",
                IEPF.Imported | IEPF.Animatable, 1.0));
            template.AddProperty(new FBXProperty<Vector3>("Color", string.Empty, "AmbientColor",
                IEPF.Imported | IEPF.Animatable, new Vector3(0.2, 0.2, 0.2)));
            template.AddProperty(new FBXProperty<double>("Number", string.Empty, "AmbientFactor",
                IEPF.Imported | IEPF.Animatable, 1.0));
            template.AddProperty(new FBXProperty<Vector3>("Color", string.Empty, "DiffuseColor",
                IEPF.Imported | IEPF.Animatable, new Vector3(0.8, 0.8, 0.8)));
            template.AddProperty(new FBXProperty<double>("Number", string.Empty, "DiffuseFactor",
                IEPF.Imported | IEPF.Animatable, 1.0));
            template.AddProperty(new FBXProperty<Vector3>("Vector3D", "Vector", "Bump", IEPF.Imported,
                new Vector3(0.0, 0.0, 0.0)));
            template.AddProperty(new FBXProperty<Vector3>("Vector3D", "Vector", "NormalMap", IEPF.Imported,
                new Vector3(0.0, 0.0, 0.0)));
            template.AddProperty(new FBXProperty<double>("double", "Number", "BumpFactor", IEPF.Imported, 1.0));
            template.AddProperty(new FBXProperty<Vector3>("Color", string.Empty, "TransparentColor",
                IEPF.Imported | IEPF.Animatable, new Vector3(0.0, 0.0, 0.0)));
            template.AddProperty(new FBXProperty<double>("Number", string.Empty, "TransparencyFactor",
                IEPF.Imported | IEPF.Animatable, 0.0));
            template.AddProperty(new FBXProperty<Vector3>("ColorRGB", "Color", "DisplacementColor", IEPF.Imported,
                new Vector3(0.0, 0.0, 0.0)));
            template.AddProperty(new FBXProperty<double>("double", "Number", "DisplacementFactor", IEPF.Imported, 1.0));
            template.AddProperty(new FBXProperty<Vector3>("ColorRGB", "Color", "VectorDisplacementColor", IEPF.Imported,
                new Vector3(0.0, 0.0, 0.0)));
            template.AddProperty(new FBXProperty<double>("double", "Number", "VectorDisplacementFactor", IEPF.Imported,
                1.0));
            template.AddProperty(new FBXProperty<Vector3>("Color", string.Empty, "SpecularColor",
                IEPF.Imported | IEPF.Animatable, new Vector3(0.2, 0.2, 0.2)));
            template.AddProperty(new FBXProperty<double>("Number", string.Empty, "SpecularFactor",
                IEPF.Imported | IEPF.Animatable, 1.0));
            template.AddProperty(new FBXProperty<double>("Number", string.Empty, "ShininessExponent",
                IEPF.Imported | IEPF.Animatable, 20.0));
            template.AddProperty(new FBXProperty<Vector3>("Color", string.Empty, "ReflectionColor",
                IEPF.Imported | IEPF.Animatable, new Vector3(0.0, 0.0, 0.0)));
            template.AddProperty(new FBXProperty<double>("Number", string.Empty, "ReflectionFactor",
                IEPF.Imported | IEPF.Animatable, 1.0));

            return template;
        }

        public static TemplateObject GetTemplateForType(FBXClassType type, IScene scene = null)
        {
            switch (type)
            {
                case FBXClassType.AnimationLayer: return GetAnimationLayerTemplate(scene);
                case FBXClassType.AnimationStack: return GetAnimationStackTemplate(scene);
                case FBXClassType.NodeAttribute: return GetNodeAttributeTemplate(scene);
                case FBXClassType.Model: return GetModelTemplate(scene);
                case FBXClassType.Geometry: return GetGeometryTemplate(scene);
                case FBXClassType.Material: return GetMaterialTemplate(scene: scene);
                case FBXClassType.Texture: return GetTextureTemplate(scene);
                case FBXClassType.Video: return GetVideoTemplate(scene);
                default: return null;
            }
        }

        public static TemplateObject GetTemplateForObject(FBXObject @object, IScene scene = null)
        {
            if (@object is null) return null;

            switch (@object.Class)
            {
                case FBXClassType.AnimationLayer: return GetAnimationLayerTemplate(scene);
                case FBXClassType.AnimationStack: return GetAnimationStackTemplate(scene);
                case FBXClassType.Model: return GetModelTemplate(scene);
                case FBXClassType.NodeAttribute: return GetNodeAttributeTemplate(scene);
                case FBXClassType.Geometry: return GetGeometryTemplate(scene);
                case FBXClassType.Material: return GetMaterialTemplate((@object as Material).ShadingModel, scene);
                case FBXClassType.Texture: return GetTextureTemplate(scene);
                case FBXClassType.Video: return GetVideoTemplate(scene);
                default: return null;
            }
        }
    }
}