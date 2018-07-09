using System;

namespace Installer.Core
{
    public class DeploymentException : Exception
    {
        public DeploymentException(string msg) : base(msg)
        {            
        }
    }
}