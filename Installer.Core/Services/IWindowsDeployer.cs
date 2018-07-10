using System;
using System.Threading.Tasks;

namespace Installer.Core.Services
{
    public interface IWindowsDeployer
    {
        Task Deploy(string imagePath, int imageIndex = 1, IObserver<double> progressObserver = null);
    }
}