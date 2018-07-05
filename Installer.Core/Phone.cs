using System.Linq;
using System.Threading.Tasks;

namespace Installer.Core
{
    public class Phone
    {
        private readonly Disk disk;

        public Phone(Disk disk)
        {
            this.disk = disk;
        }

        public async Task<Volume> GetVolume(string label) => (await disk.GetVolumes()).Single(volume => string.Equals(volume.Label, label));
        public Task<Volume> GetEfiespVolume() => GetVolume("EFIESP");
        public Task<Volume> GetWindowsVolume() => GetVolume("WindowsARM");
        public Task<Volume> GetBootVolume() => GetVolume("BOOT");
    }
}