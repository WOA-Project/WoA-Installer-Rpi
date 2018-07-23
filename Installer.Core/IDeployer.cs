using System;
using System.Threading.Tasks;

namespace Installer.Core
{
    public interface IDeployer<in TDevice> where TDevice : Device
    {
        Task DeployCoreAndWindows(InstallOptions options, TDevice device, IObserver<double> progressObserver = null);
        Task DeployWindows(InstallOptions options, TDevice device, IObserver<double> progressObserver = null);
        Task InjectPostOobeDrivers(TDevice device);
    }
}