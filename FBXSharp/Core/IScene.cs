using System.Collections.Generic;

namespace FBXSharp.Core
{
    public interface IScene
    {
        FBXObject Root { get; }
        GlobalSettings Settings { get; }
        IReadOnlyList<FBXObject> Objects { get; }
        IReadOnlyList<TakeInfo> TakeInfos { get; }
        IReadOnlyList<TemplateObject> Templates { get; }

        TemplateObject CreateEmptyTemplate(FBXClassType classType, TemplateCreationType creationType);
        TemplateObject CreatePredefinedTemplate(FBXClassType classType, TemplateCreationType creationType);

        TemplateObject GetTemplateObject(string name);
        TemplateObject GetTemplateObject(FBXClassType classType);

        void AddTakeInfo(TakeInfo takeInfo);
        void RemoveTakeInfo(TakeInfo takeInfo);

        FBXObject CreateFBXObject(FBXClassType classType, FBXObjectType objectType, IElement element);
        void DestroyFBXObject(FBXObject @object);

        IEnumerable<T> GetObjectsOfType<T>() where T : FBXObject;
        IEnumerable<FBXObject> GetObjectsOfType(FBXClassType classType, FBXObjectType objectType);
    }
}