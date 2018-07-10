using System;

namespace Installer.Core.Exceptions
{
    public class NotEnoughSpaceException : Exception
    {
        public NotEnoughSpaceException(string msg) : base(msg)
        {
        }
    }
}