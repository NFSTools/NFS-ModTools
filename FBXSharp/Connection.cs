using FBXSharp.Core;

namespace FBXSharp
{
    public class Connection
    {
        public enum ConnectionType
        {
            Object,
            Property
        }

        public ConnectionType Type { get; }
        public long Source { get; }
        public long Destination { get; }
        public IElementAttribute Property { get; }

        public Connection(ConnectionType type, long src, long dest, IElementAttribute property = null)
        {
            Type = type;
            Source = src;
            Destination = dest;
            Property = property;
        }
    }
}