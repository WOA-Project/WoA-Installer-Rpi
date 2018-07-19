using System.Threading.Tasks;

namespace Installer.Core
{
    public interface ICoreDeployer
    {
        Task<bool> AreDeploymentFilesValid();
        Task Deploy(Phone phone);
    }
}