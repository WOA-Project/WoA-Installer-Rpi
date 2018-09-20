using System;

namespace Installer.Core.Exceptions
{
    public class PhoneDiskNotFoundException : Exception
    {
        public PhoneDiskNotFoundException(string message)  : base(message)
    }
}
