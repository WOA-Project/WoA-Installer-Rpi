using System;

namespace Installer.Core
{
    internal class PathNotFoundException : Exception
    {
        public PathNotFoundException(string msg) : base(msg)
        {
        }
    }
}