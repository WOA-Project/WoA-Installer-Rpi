using System.Threading.Tasks;

namespace Installer.Core
{
    public interface IConfigProvider
    {
        Task<Config> Retrieve();
    }
}