using System;
using System.IO;

namespace Installer.Core.Exceptions
{
    public class InvalidWimFileException : Exception
    {
        public InvalidWimFileException(string msg) : base(msg)
        {            
        }

        public InvalidWimFileException(string msg, Exception innerException) : base(msg, innerException)
        {            
        }
    }
}