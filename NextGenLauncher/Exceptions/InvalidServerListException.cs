using System;

namespace NextGenLauncher.Exceptions
{
    public class InvalidServerListException : Exception
    {
        public InvalidServerListException()
        {
        }

        public InvalidServerListException(string message) : base(message)
        {
        }

        public InvalidServerListException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}