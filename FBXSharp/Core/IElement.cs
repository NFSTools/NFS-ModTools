namespace FBXSharp.Core
{
    public interface IElement
    {
        string Name { get; }
        IElement[] Children { get; }
        IElementAttribute[] Attributes { get; }

        IElement FindChild(string name);
    }
}