using System;

namespace NextGenLauncher.Exceptions
{
    public class ModSystemException : Exception
    {
        public ModSystemException()
        {
        }

        public ModSystemException(string message) : base(message)
        {
        }

        public ModSystemException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}