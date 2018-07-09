using System;

namespace Installer.Core.FullFx
{
    internal class InvalidImageException : Exception
    {
        public InvalidImageException(string msg) : base(msg)
        {            
        }
    }
}