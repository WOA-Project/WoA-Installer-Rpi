using System.Threading.Tasks;

namespace Installer.Core
{
    public interface IWindowsDeployer
    {
        Task Deploy(string imagePath);
    }
}