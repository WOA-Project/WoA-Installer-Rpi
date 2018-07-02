using System.Threading.Tasks;

namespace Installer.Core
{
    public interface IWindowsImageService
    {
        Task ApplyImage(string imagePath, Partition windowsPartition);
        Task InjectDrivers(string path, Partition windowsPartition);
    }
}