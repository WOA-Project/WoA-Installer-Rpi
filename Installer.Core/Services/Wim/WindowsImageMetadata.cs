using System.Collections.Generic;

namespace Installer.Core.Services.Wim
{
    public class WindowsImageMetadata
    {
        public IList<DiskImageMetadata> Images { get; set; }
    }
}