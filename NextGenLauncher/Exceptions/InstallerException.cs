using System;

namespace NextGenLauncher.Exceptions
{
    public class InstallerException : Exception
    {
        public InstallerException()
        {
        }

        public InstallerException(string message) : base(message)
        {
        }

        public InstallerException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}