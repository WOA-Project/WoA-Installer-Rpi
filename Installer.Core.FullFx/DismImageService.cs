using System;
using System.Threading.Tasks;

namespace Installer.Core.FullFx
{
    public class DismImageService : IWindowsImageService
    {
        public Task ApplyImage(string imagePath, Partition windowsPartition)
        {
            throw new NotImplementedException();
        }

        public Task InjectDrivers(string path, Partition windowsPartition)
        {
            throw new NotImplementedException();
        }
    }
}