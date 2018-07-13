using System;
using System.Threading.Tasks;

namespace Installer.Core.Services
{
    public interface IWindowsDeployer
    {
        Task Deploy(InstallOptions options, Phone phone, IObserver<double> progressObserver = null);
        Task MakeBootable(WindowsVolumes volumes);
        Task InjectPostOobeDrivers(Phone phone);
        Task<bool> AreDeploymentFilesValid();
    }
}