using System;

namespace Installer.Core.FullFx
{
    public class PhoneDiskNotFoundException : Exception
    {
        public PhoneDiskNotFoundException(string message)  : base(message)
        {
            
        }
    }
}