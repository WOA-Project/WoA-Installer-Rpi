using System.Threading.Tasks;

namespace Install
{
    public interface ISetup
    {
        Task FullInstall(InstallOptions options);
    }
}