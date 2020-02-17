using System;
using System.Runtime.Serialization;

namespace NextGenLauncher.Proxy
{
    [Serializable]
    public class ServerProxyException : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public ServerProxyException()
        {
        }

        public ServerProxyException(string message) : base(message)
        {
        }

        public ServerProxyException(string message, Exception inner) : base(message, inner)
        {
        }

        protected ServerProxyException(
            SerializationInfo info,
            StreamingContext context) : base(info, context)
        {
        }
    }
}