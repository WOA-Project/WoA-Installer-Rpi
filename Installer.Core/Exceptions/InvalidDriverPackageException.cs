using System;

namespace Installer.Core.Exceptions
{
    internal class InvalidDriverPackageException : Exception
    {
        public InvalidDriverPackageException(string msg) : base(msg)
        {            
        }
    }
}