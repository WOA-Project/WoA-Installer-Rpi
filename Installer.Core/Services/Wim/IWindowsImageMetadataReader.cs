using System.IO;

namespace Installer.Core.Services.Wim
{
    public interface IWindowsImageMetadataReader
    {
        XmlWindowsImageMetadata Load(Stream stream);
    }
}