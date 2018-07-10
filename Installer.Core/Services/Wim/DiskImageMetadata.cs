using System.Runtime.InteropServices;

namespace Installer.Core.Services.Wim
{
    public class DiskImageMetadata
    {
        public int Index { get; set; }
        public string DisplayName { get; set; }
        public Architecture Architecture { get; set; }
        public string Build { get; set; }
    }
}