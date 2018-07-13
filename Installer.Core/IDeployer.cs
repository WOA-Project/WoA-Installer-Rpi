using System;
using System.Threading.Tasks;

namespace Installer.Core
{
    public interface IDeployer
    {
        Task DeployCoreAndWindows(InstallOptions options, Phone phone, IObserver<double> progressObserver = null);
        Task DeployWindows(InstallOptions options, Phone phone, IObserver<double> progressObserver = null);
        Task InjectPostOobeDrivers(Phone phone);
    }
}