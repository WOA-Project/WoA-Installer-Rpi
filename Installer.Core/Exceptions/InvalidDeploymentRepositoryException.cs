using System;

namespace Installer.Core.Exceptions
{
    internal class InvalidDeploymentRepositoryException : Exception
    {
        public InvalidDeploymentRepositoryException(string str) : base(str)
        {            
        }

        public InvalidDeploymentRepositoryException(string str, Exception inner) : base(str, inner)
        {            
        }
    }
}