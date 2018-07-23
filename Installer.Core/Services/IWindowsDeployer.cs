using System;
using System.Threading.Tasks;

namespace Installer.Core.Services
{
    public interface IWindowsDeployer<in TDevice> where TDevice : Device
    {
        Task Deploy(InstallOptions options, TDevice device, IObserver<double> progressObserver = null);
        Task InjectPostOobeDrivers(TDevice phone);
        Task<bool> AreDeploymentFilesValid();
    }
}