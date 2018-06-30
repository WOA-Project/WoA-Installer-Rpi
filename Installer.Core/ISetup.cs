using System.Threading.Tasks;

namespace Installer.Core
{
    public interface ISetup
    {
        Task FullInstall(InstallOptions options);
    }
}