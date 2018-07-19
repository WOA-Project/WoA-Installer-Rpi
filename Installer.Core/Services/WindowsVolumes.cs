using Installer.Core.FileSystem;

namespace Installer.Core.Services
{
    public class WindowsVolumes
    {
        public WindowsVolumes(Volume bootVolume, Volume windowsVolume)
        {
            Boot = bootVolume;
            Windows = windowsVolume;
        }

        public Volume Boot { get; }
        public Volume Windows { get; }
    }
}