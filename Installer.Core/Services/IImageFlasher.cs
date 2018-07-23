using System;
using System.Threading.Tasks;
using Installer.Core.FileSystem;

namespace Installer.Core.Services
{
    public interface IImageFlasher
    {
        Task Flash(Disk disk, string imagePath, IObserver<double> progressObserver = null);
    }
}