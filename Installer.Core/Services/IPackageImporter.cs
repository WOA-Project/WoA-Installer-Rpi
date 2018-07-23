using System;
using System.Threading.Tasks;

namespace Installer.Core.Services
{
    public interface IPackageImporter
    {
        Task Extract(string packagePath, IObserver<double> progressObserver = null);
        Task<string> GetReadmeText(string fileName);
    }
}