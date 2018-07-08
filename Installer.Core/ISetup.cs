using System;
using System.Threading.Tasks;

namespace Installer.Core
{
    public interface ISetup
    {
        Task DeployUefiAndWindows(InstallOptions options, IObserver<double> progressObserver);
        Task DeployWindows(InstallOptions options, IObserver<double> progressObserver);
    }
}