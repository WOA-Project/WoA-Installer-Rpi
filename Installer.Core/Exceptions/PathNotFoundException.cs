using System;

namespace Installer.Core.Exceptions
{
    internal class PathNotFoundException : Exception
    {
        public PathNotFoundException(string msg) : base(msg)
        {
        }
    }
}