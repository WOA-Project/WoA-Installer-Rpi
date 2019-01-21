using System.Threading.Tasks;
using Installer.Core;
using Installer.Core.FileSystem;

namespace Installer.Raspberry.Core
{
    public class RaspberryPi : Device
    {
        public RaspberryPi(Disk disk) : base(disk)
        {
        }

        public override async Task RemoveExistingWindowsPartitions()
        {
            await Task.CompletedTask;
        }

        public override Task<Volume> GetBootVolume()
        {
            return GetVolume("EFIESP");
        }
    }
}