using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Registry;
using Serilog;

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

        public static async Task<Phone> Load(ILowLevelApi lowLevelApi)
        {
            var disk = await lowLevelApi.GetPhoneDisk();
            return new Phone(disk);
        }

        public async Task<DualBootStatus> GetDualBootStatus()
        {
            var isWoaPresent = await IsWoAPresent();
            var isOobeComplete = await IsObeeCompleted();
            var boolVolume = await GetBootVolume();

            var isEnabled = boolVolume.Partition.PartitionType == PartitionType.Basic;

            var canBeEnabled = isWoaPresent && isOobeComplete;
            return new DualBootStatus(canBeEnabled, isEnabled);
        }

        private async Task<bool> IsObeeCompleted()
        {
            var windowsVolume = await GetWindowsVolume();
            var path = Path.Combine(windowsVolume.RootDir.Name, "Windows", "System32", "Config", "System");
            var hive = new RegistryHive(path) {RecoverDeleted = true};
            hive.ParseHive();

            var key = hive.GetKey("Setup");
            var val = key.Values.Single(x => x.ValueName == "OOBEInProgress");

            return int.Parse(val.ValueData) == 0;
        }

        private async Task<bool> IsWoAPresent()
        {
            try
            {
                await GetBootVolume();
                await GetWindowsVolume();
            }
            catch (Exception e)
            {
                Log.Error(e, "Failed to get WoA's volumes");
                return false;
            }

            return true;
        }

        public async Task EnableDualBoot(bool enable)
        {
            var status = await GetDualBootStatus();
            if (!status.CanDualBoot)
            {
                throw new InvalidOperationException("Cannot enable Dual Boot");
            }

            if (status.IsEnabled != enable)
            {
                if (enable)
                {
                    await EnableDualBoot();
                }
                else
                {
                    await DisableDualBoot();
                }
            }            
        }

        private async Task EnableDualBoot()
        {
            var winPhoneBcdGuid = Guid.Parse("7619dcc9-fafe-11d9-b411-000476eba25f");

            var vol = await GetBootVolume();
            await vol.Partition.SetGptType(PartitionType.Basic);
            var bcdInvoker = new BcdInvoker((await GetEfiespVolume()).GetBcdFullFilename());
            bcdInvoker.Invoke($@"/set {{{winPhoneBcdGuid}}} description ""Windows 10 Phone""");
            bcdInvoker.Invoke($@"/displayorder {{{winPhoneBcdGuid}}} /addlast");
        }

        private async Task DisableDualBoot()
        {
            var winPhoneBcdGuid = Guid.Parse("7619dcc9-fafe-11d9-b411-000476eba25f");

            var vol = await GetBootVolume();
            await vol.Partition.SetGptType(PartitionType.Esp);
            var bcdInvoker = new BcdInvoker((await GetEfiespVolume()).GetBcdFullFilename());
            bcdInvoker.Invoke($@"/displayorder {{{winPhoneBcdGuid}}} /remove");
        }
    }
}