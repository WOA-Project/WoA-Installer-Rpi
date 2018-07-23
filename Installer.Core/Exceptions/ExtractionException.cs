using System;

namespace Installer.Core.Exceptions
{
    internal class ExtractionException : Exception
    {
        public ExtractionException(string msg) : base(msg)
        {            
        }
    }
}