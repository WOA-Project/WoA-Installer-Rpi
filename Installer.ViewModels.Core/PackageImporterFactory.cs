using System.Collections.Generic;
using System.Collections.Immutable;
using Installer.Core.Services;
using Installer.Core.Services.Packages;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Archives.Zip;
using SharpCompress.Common.SevenZip;
using SharpCompress.Common.Zip;

namespace Installer.ViewModels.Core
{
    public class PackageImporterFactory : IPackageImporterFactory
    {
        readonly Dictionary<string, IPackageImporter> importers = new Dictionary<string, IPackageImporter>();

        public PackageImporterFactory()
        {
            importers.Add("7z", new PackageImporter(new ArchiveUncompressor<SevenZipArchiveEntry, SevenZipVolume, SevenZipArchive>(
                s => SevenZipArchive.Open(s))));
            importers.Add("zip", new PackageImporter(new ArchiveUncompressor<ZipArchiveEntry, ZipVolume, ZipArchive>(s => ZipArchive.Open(s))));
        }

        public IPackageImporter GetImporter(string fileType)
        {
            return importers[fileType];
        }

        public ICollection<string> ImporterKeys => importers.Keys.ToImmutableList();
    }
}