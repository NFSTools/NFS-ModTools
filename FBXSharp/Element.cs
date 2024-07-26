using System;
using System.Diagnostics;
using FBXSharp.Core;

namespace FBXSharp
{
    [DebuggerDisplay("{this.Name}")]
    public class Element : IElement
    {
        public string Name { get; }
        public IElement[] Children { get; }
        public IElementAttribute[] Attributes { get; }

        public Element(string name, IElement[] children, IElementAttribute[] attributes)
        {
            Name = name ?? string.Empty;
            Children = children ?? Array.Empty<IElement>();
            Attributes = attributes ?? Array.Empty<IElementAttribute>();
        }

        public IElement FindChild(string name)
        {
            return Array.Find(Children, _ => _.Name == name);
        }

        public static Element WithAttribute(string name, IElementAttribute attribute)
        {
            return new Element(name, null, new[] { attribute });
        }
    }
}