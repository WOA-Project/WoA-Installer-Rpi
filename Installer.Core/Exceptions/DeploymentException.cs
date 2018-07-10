using System;

namespace Installer.Core.Exceptions
{
    public class DeploymentException : Exception
    {
        public DeploymentException(string msg) : base(msg)
        {            
        }
    }
}