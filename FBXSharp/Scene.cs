using System;
using System.Collections.Generic;
using FBXSharp.Core;
using FBXSharp.Objective;

namespace FBXSharp
{
    public class Scene : IScene
    {
        private readonly List<FBXObject> m_objects;
        private readonly List<TakeInfo> m_takeInfos;
        private readonly List<TemplateObject> m_templates;
        private readonly Dictionary<(FBXClassType, FBXObjectType), Func<IElement, FBXObject>> m_activatorMap;

        public Root RootNode { get; }

        public FBXObject Root => RootNode;

        public GlobalSettings Settings { get; }

        public IReadOnlyList<FBXObject> Objects => m_objects;

        public IReadOnlyList<TakeInfo> TakeInfos => m_takeInfos;

        public IReadOnlyList<TemplateObject> Templates => m_templates;

        public Scene()
        {
            RootNode = new Root(this);
            m_objects = new List<FBXObject>();
            m_takeInfos = new List<TakeInfo>();
            m_templates = new List<TemplateObject>();
            Settings = new GlobalSettings(null, this);

            m_activatorMap = new Dictionary<(FBXClassType, FBXObjectType), Func<IElement, FBXObject>>
            {
                // AnimationCurve
                {
                    (FBXClassType.AnimationCurve, FBXObjectType.AnimationCurve),
                    element => new AnimationCurve(element, this)
                },

                // AnimationCurveNode
                {
                    (FBXClassType.AnimationCurveNode, FBXObjectType.AnimationCurveNode),
                    element => new AnimationCurveNode(element, this)
                },

                // AnimationLayer
                {
                    (FBXClassType.AnimationLayer, FBXObjectType.AnimationLayer),
                    element => new AnimationLayer(element, this)
                },

                // AnimationStack
                {
                    (FBXClassType.AnimationStack, FBXObjectType.AnimationStack),
                    element => new AnimationStack(element, this)
                },

                // Deformer
                { (FBXClassType.Deformer, FBXObjectType.BlendShape), element => new BlendShape(element, this) },
                {
                    (FBXClassType.Deformer, FBXObjectType.BlendShapeChannel),
                    element => new BlendShapeChannel(element, this)
                },
                { (FBXClassType.Deformer, FBXObjectType.Cluster), element => new Cluster(element, this) },
                { (FBXClassType.Deformer, FBXObjectType.Skin), element => new Skin(element, this) },

                // Geometry
                { (FBXClassType.Geometry, FBXObjectType.Mesh), element => new Geometry(element, this) },
                { (FBXClassType.Geometry, FBXObjectType.Shape), element => new Shape(element, this) },

                // Material
                { (FBXClassType.Material, FBXObjectType.Material), element => new Material(element, this) },

                // Model
                { (FBXClassType.Model, FBXObjectType.Camera), element => new Camera(element, this) },
                { (FBXClassType.Model, FBXObjectType.Light), element => new Light(element, this) },
                { (FBXClassType.Model, FBXObjectType.LimbNode), element => new LimbNode(element, this) },
                { (FBXClassType.Model, FBXObjectType.Mesh), element => new Mesh(element, this) },
                { (FBXClassType.Model, FBXObjectType.Null), element => new NullNode(element, this) },

                // NodeAttribute
                { (FBXClassType.NodeAttribute, FBXObjectType.Camera), element => new CameraAttribute(element, this) },
                { (FBXClassType.NodeAttribute, FBXObjectType.Light), element => new LightAttribute(element, this) },
                {
                    (FBXClassType.NodeAttribute, FBXObjectType.LimbNode),
                    element => new LimbNodeAttribute(element, this)
                },
                { (FBXClassType.NodeAttribute, FBXObjectType.Null), element => new NullAttribute(element, this) },

                // Pose
                { (FBXClassType.Pose, FBXObjectType.BindPose), element => new BindPose(element, this) },

                // Texture
                { (FBXClassType.Texture, FBXObjectType.Texture), element => new Texture(element, this) },

                // Video
                { (FBXClassType.Video, FBXObjectType.Clip), element => new Clip(element, this) }
            };
        }

        private T AddObjectAndReturn<T>(T value) where T : FBXObject
        {
            m_objects.Add(value);
            return value;
        }

        internal void InternalAddObject(FBXObject @object)
        {
            m_objects.Add(@object);
        }

        internal void InternalSetTakeInfos(TakeInfo[] takeInfos)
        {
            m_takeInfos.AddRange(takeInfos ?? Array.Empty<TakeInfo>());
        }

        internal void InternalSetTemplates(TemplateObject[] templates)
        {
            m_templates.AddRange(templates ?? Array.Empty<TemplateObject>());
        }

        public void AddTakeInfo(TakeInfo takeInfo)
        {
            m_takeInfos.Add(takeInfo);
        }

        public void RemoveTakeInfo(TakeInfo takeInfo)
        {
            m_takeInfos.Remove(takeInfo);
        }

        public TemplateObject GetTemplateObject(string name)
        {
            return m_templates.Find(_ => _.Name == name);
        }

        public TemplateObject GetTemplateObject(FBXClassType classType)
        {
            return m_templates.Find(_ => _.OverridableType == classType);
        }

        public TemplateObject CreateEmptyTemplate(FBXClassType classType, TemplateCreationType creationType)
        {
            var template = m_templates.Find(_ => _.OverridableType == classType);

            if (template is null)
            {
                m_templates.Add(template = new TemplateObject(classType, null, this));

                return template;
            }

            if (creationType == TemplateCreationType.NewOverrideAnyExisting)
            {
                template.RemoveAllProperties();
                template.Name = string.Empty;
            }

            return template;
        }

        public TemplateObject CreatePredefinedTemplate(FBXClassType classType, TemplateCreationType creationType)
        {
            var indexer = m_templates.FindIndex(_ => _.OverridableType == classType);

            if (indexer < 0)
            {
                var template = TemplateFactory.GetTemplateForType(classType, this);

                m_templates.Add(template);

                return template;
            }

            if (creationType == TemplateCreationType.DontCreateIfDuplicated) return m_templates[indexer];

            if (creationType == TemplateCreationType.NewOverrideAnyExisting)
            {
                m_templates[indexer] = TemplateFactory.GetTemplateForType(classType, this);

                return m_templates[indexer];
            }

            if (creationType == TemplateCreationType.MergeIfExistingIsFound)
            {
                m_templates[indexer].MergeWith(TemplateFactory.GetTemplateForType(classType));

                return m_templates[indexer];
            }

            throw new Exception("Template creation type passed is invalid");
        }

        public FBXObject CreateFBXObject(FBXClassType classType, FBXObjectType objectType, IElement element = null)
        {
            if (m_activatorMap.TryGetValue((classType, objectType), out var activator))
                return AddObjectAndReturn(activator(element));

            return null;
        }

        public void DestroyFBXObject(FBXObject @object)
        {
            if (@object is null) return;

            if (m_objects.Remove(@object)) @object.Destroy();
        }

        public void RegisterObjectType<T>(FBXClassType classType, FBXObjectType objectType) where T : FBXObject
        {
            m_activatorMap[(classType, objectType)] = element => (T)Activator.CreateInstance(typeof(T), element, this);
        }

        public void RegisterObjectType<T>(FBXClassType classType, FBXObjectType objectType, Func<IElement, T> activator)
            where T : FBXObject
        {
            if (activator is null)
                RegisterObjectType<T>(classType, objectType);
            else
                m_activatorMap[(classType, objectType)] = activator;
        }

        public Clip CreateVideo()
        {
            return AddObjectAndReturn(new Clip(null, this));
        }

        public Texture CreateTexture()
        {
            return AddObjectAndReturn(new Texture(null, this));
        }

        public Material CreateMaterial()
        {
            return AddObjectAndReturn(new Material(null, this));
        }

        public Geometry CreateGeometry()
        {
            return AddObjectAndReturn(new Geometry(null, this));
        }

        public Shape CreateShape()
        {
            return AddObjectAndReturn(new Shape(null, this));
        }

        public BindPose CreateBindPose()
        {
            return AddObjectAndReturn(new BindPose(null, this));
        }

        public Cluster CreateCluster()
        {
            return AddObjectAndReturn(new Cluster(null, this));
        }

        public Skin CreateSkin()
        {
            return AddObjectAndReturn(new Skin(null, this));
        }

        public BlendShape CreateBlendShape()
        {
            return AddObjectAndReturn(new BlendShape(null, this));
        }

        public BlendShapeChannel CreateBlendShapeChannel()
        {
            return AddObjectAndReturn(new BlendShapeChannel(null, this));
        }

        public AnimationStack CreateAnimationStack()
        {
            return AddObjectAndReturn(new AnimationStack(null, this));
        }

        public AnimationLayer CreateAnimationLayer()
        {
            return AddObjectAndReturn(new AnimationLayer(null, this));
        }

        public AnimationCurve CreateAnimationCurve()
        {
            return AddObjectAndReturn(new AnimationCurve(null, this));
        }

        public AnimationCurveNode CreateAnimationCurveNode()
        {
            return AddObjectAndReturn(new AnimationCurveNode(null, this));
        }

        public Mesh CreateMesh()
        {
            return AddObjectAndReturn(new Mesh(null, this));
        }

        public Light CreateLight()
        {
            return AddObjectAndReturn(new Light(null, this));
        }

        public Camera CreateCamera()
        {
            return AddObjectAndReturn(new Camera(null, this));
        }

        public NullNode CreateNullNode()
        {
            return AddObjectAndReturn(new NullNode(null, this));
        }

        public NullAttribute CreateNullAttribute()
        {
            return AddObjectAndReturn(new NullAttribute(null, this));
        }

        public LightAttribute CreateLightAttribute()
        {
            return AddObjectAndReturn(new LightAttribute(null, this));
        }

        public CameraAttribute CreateCameraAttribute()
        {
            return AddObjectAndReturn(new CameraAttribute(null, this));
        }

        public LimbNodeAttribute CreateLimbNodeAttribute()
        {
            return AddObjectAndReturn(new LimbNodeAttribute(null, this));
        }

        public IEnumerable<T> GetObjectsOfType<T>() where T : FBXObject
        {
            for (var i = 0; i < m_objects.Count; ++i)
                if (m_objects[i] is T @object)
                    yield return @object;
        }

        public IEnumerable<FBXObject> GetObjectsOfType(FBXClassType classType, FBXObjectType objectType)
        {
            for (var i = 0; i < m_objects.Count; ++i)
            {
                var @object = m_objects[i];

                if (@object.Class == classType && @object.Type == objectType) yield return m_objects[i];
            }
        }
    }
}