using System.Collections.Generic;
using Installer.Core.Services;

namespace Installer.ViewModels.Core
{
    public interface IPackageImporterFactory
    {
        IPackageImporter GetImporter(string fileType);
        ICollection<string> ImporterKeys { get; }
    }
}