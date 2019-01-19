using System;
using System.Threading.Tasks;
using Installer.Core.FileSystem;

namespace Installer.Core.Services
{
    public interface IWindowsImageService
    {
        Task ApplyImage(Volume windowsVolume, string imagePath, int imageIndex = 1, IObserver<double> progressObserver = null);
        Task InjectDrivers(string path, Volume windowsPartition);
        Task RemoveDriver(string path, Volume volume);
    }
}