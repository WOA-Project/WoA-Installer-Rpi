using System;

namespace Installer.Core
{
    public class NotEnoughSpaceException : Exception
    {
        public NotEnoughSpaceException(string msg) : base(msg)
        {
        }
    }
}