using System;

namespace Intaller.Wpf
{
    internal class InvalidWimFileException : Exception
    {
        public InvalidWimFileException(string msg) : base(msg)
        {            
        }
    }
}