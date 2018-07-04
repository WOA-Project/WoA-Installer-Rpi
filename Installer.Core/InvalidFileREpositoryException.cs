using System;

namespace Installer.Core
{
    internal class InvalidFileRepositoryException : Exception
    {
        public InvalidFileRepositoryException(string str) : base(str)
        {            
        }
    }
}