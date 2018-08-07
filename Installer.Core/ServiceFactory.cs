using System;
using Installer.Core.Services;

namespace Installer.Core
{
    public abstract class ServiceFactory
    {
        private static ServiceFactory current;
        public IWindowsImageService ImageService { get; protected set; }
        public DiskService DiskService { get; protected set; }

        public static ServiceFactory Current
        {
            get
            {
                if (current == null)
                {
                    throw new InvalidOperationException("Please, set the Current property of the ServiceFactory");
                }

                return current;
            }
            set => current = value;
        }
    }
}