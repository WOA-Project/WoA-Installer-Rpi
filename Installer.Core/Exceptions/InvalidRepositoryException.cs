using System;

namespace Installer.Core.Exceptions
{
    internal class InvalidRepositoryException : Exception
    {
        public InvalidRepositoryException(string str) : base(str)
        {            
        }
    }
}