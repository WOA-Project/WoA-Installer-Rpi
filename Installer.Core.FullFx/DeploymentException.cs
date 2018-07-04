using System;

namespace Installer.Core.FullFx
{
    public class DeploymentException : Exception
    {
        public DeploymentException(string msg) : base(msg)
        {            
        }
    }
}