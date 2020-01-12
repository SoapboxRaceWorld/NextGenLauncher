using System;

namespace NextGenLauncher.Exceptions
{
    public class PlayException : Exception
    {
        public PlayException()
        {
        }

        public PlayException(string message) : base(message)
        {
        }

        public PlayException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}