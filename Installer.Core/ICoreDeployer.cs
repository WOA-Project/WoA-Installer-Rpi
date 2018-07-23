using System.Threading.Tasks;

namespace Installer.Core
{
    public interface ICoreDeployer<TDevice> where TDevice : Device
    {
        Task<bool> AreDeploymentFilesValid();
        Task Deploy(TDevice phone);
    }
}