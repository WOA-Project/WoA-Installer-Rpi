using System;
using System.IO;

namespace Installer.Core.Services
{
    public static class SystemPaths
    {
        public static string BcdEdit { get; } = Path.Combine(GetSystemFolder, "bcdedit.exe");
        public static string Dism { get; } = Path.Combine(GetSystemFolder, "dism.exe");

        private static string GetSystemFolder
        {
            get
            {
                var shouldUseSysNative = Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess;

                if (shouldUseSysNative)
                {
                    return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "SysNative");
                }

                return Path.Combine(Environment.SystemDirectory);
            }
        }
    }
}