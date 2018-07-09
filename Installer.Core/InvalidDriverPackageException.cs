using System;

namespace Installer.Core
{
    internal class InvalidDriverPackageException : Exception
    {
        public InvalidDriverPackageException(string msg) : base(msg)
        {            
        }
    }
}