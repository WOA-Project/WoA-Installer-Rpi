using System;
using System.Threading.Tasks;

namespace Installer.Core.Services
{
    public interface IDriverPackageImporter
    {
        Task ImportDriverPackage(string packagePath, string destination, IObserver<double> progressObserver = null);
        Task<string> GetReadmeText(string fileName);
    }
}