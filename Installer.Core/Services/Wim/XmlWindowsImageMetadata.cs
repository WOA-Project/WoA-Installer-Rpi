using System.Collections.Generic;

namespace Installer.Core.Services.Wim
{
    public class XmlWindowsImageMetadata
    {
        public IList<DiskImageMetadata> Images { get; set; }
    }
}