using System;
using System.Runtime.Serialization;

namespace CompLib
{
    [Serializable]
    public class CompressionException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public CompressionException()
        {
        }

        public CompressionException(string message) : base(message)
        {
        }

        public CompressionException(string message, Exception inner) : base(message, inner)
        {
        }

        protected CompressionException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}