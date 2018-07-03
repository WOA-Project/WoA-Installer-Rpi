using System;
using System.Threading.Tasks;

namespace Installer.Core
{
    public interface IWindowsImageService
    {
        Task ApplyImage(Volume windowsVolume, string imagePath, int imageIndex = 1, IObserver<double> progressObserver = null);
        Task InjectDrivers(string path, Volume windowsPartition);
    }
}