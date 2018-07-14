using Installer.Core.Services;

namespace Intaller.Wpf
{
    public class DriverPackageImporterItem
    {
        public DriverPackageImporterItem(string extension,  IDriverPackageImporter importer)
        {
            Extension = extension;
            DriverPackageImporter = importer;
        }

        public string Extension { get; }
        public IDriverPackageImporter DriverPackageImporter { get; }
    }
}