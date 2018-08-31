using System;
using System.Threading.Tasks;

namespace Installer.Core.Services.Packages
{
    public class PackageImporter : IPackageImporter
    {
        private readonly IArchiveUncompressor uncompressor;

        public PackageImporter(IArchiveUncompressor uncompressor)
        {
            this.uncompressor = uncompressor;
        }

        public Task Extract(string packagePath, IObserver<double> progressObserver = null)
        {
            return uncompressor.Extract(packagePath, "Files", progressObserver);
        }

        public Task<string> GetReadmeText(string fileName)
        {
            return uncompressor.ReadToEnd(fileName, "Readme.txt");
        }
    }
}
