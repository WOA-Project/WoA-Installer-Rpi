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
        private Volume windowsVolume;
        private Volume efiEspVolume;
        private Volume boolVolume;

        public Phone(Disk disk)
        {
            this.disk = disk;
        }

        private async Task<Volume> GetVolume(string label)
        {
            var volumes = await disk.GetVolumes();
            var volume = volumes.FirstOrDefault(v => string.Equals(v.Label, label));

            if (volume != null)
            {
                await volume.Mount();
            }

            return volume;
        }

        public async Task<Volume> GetEfiespVolume() => efiEspVolume ?? (efiEspVolume = await GetVolume("EFIESP"));
        public async Task<Volume> GetWindowsVolume() => windowsVolume ?? (windowsVolume = await GetVolume("WindowsARM"));
        public async Task<Volume> GetBootVolume() => boolVolume ?? (boolVolume = await GetVolume("BOOT"));

        public static async Task<Phone> Load(ILowLevelApi lowLevelApi)
        {
            var disk = await lowLevelApi.GetPhoneDisk();
            return new Phone(disk);
        }

        public async Task<DualBootStatus> GetDualBootStatus()
        {
            Log.Information("Getting Dual Boot Status...");
            
            var isWoaPresent = await IsWoAPresent();
            var isWPhonePresent = await IsWindowsPhonePresent();
            var isOobeComplete = IsObeeCompleted();

            var isEnabled = await GetBootPartition() == null;

            var isCapable = isWoaPresent && isWPhonePresent && isOobeComplete;
            var status = new DualBootStatus(isCapable, isEnabled);
            

            Log.Information("Dual Boot Status retrieved");
            Log.Verbose("Dual Boot Status is {@Status}", status);

            return status;
        }

        private bool IsObeeCompleted()
        {
            var path = Path.Combine(windowsVolume.RootDir.Name, "Windows", "System32", "Config", "System");
            var hive = new RegistryHive(path) { RecoverDeleted = true };
            hive.ParseHive();

            var key = hive.GetKey("Setup");
            var val = key.Values.Single(x => x.ValueName == "OOBEInProgress");

            return int.Parse(val.ValueData) == 0;
        }

        private async Task<bool> IsWoAPresent()
        {
            try
            {
                await IsBootVolumePresent();
                await GetWindowsVolume();
            }
            catch (Exception e)
            {
                Log.Error(e, "Failed to get WoA's volumes");
                return false;
            }

            return true;
        }

        private async Task<bool> IsBootVolumePresent()
        {
            var bootPartition = await GetBootPartition();

            if (bootPartition != null)
            {
                return true;
            }

            var bootVolume = await GetBootVolume();
            return bootVolume != null;
        }

        private async Task<bool> IsWindowsPhonePresent()
        {
            try
            {
                await GetVolume("MainOS");
                await GetVolume("Data");
            }
            catch (Exception e)
            {
                Log.Error(e, "Failed to get Windows Phones's volumes");
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
            Log.Information("Enabling Dual Boot...");

            var winPhoneBcdGuid = Guid.Parse("7619dcc9-fafe-11d9-b411-000476eba25f");

            var bootPartition = await GetBootPartition();
            await bootPartition.SetGptType(PartitionType.Basic);
            var volume = await GetEfiespVolume();
            var bcdInvoker = new BcdInvoker(volume.GetBcdFullFilename());
            bcdInvoker.Invoke($@"/set {{{winPhoneBcdGuid}}} description ""Windows 10 Phone""");
            bcdInvoker.Invoke($@"/displayorder {{{winPhoneBcdGuid}}} /addlast");

            Log.Information("Dual Boot enabled");
        }

        private async Task DisableDualBoot()
        {
            Log.Information("Disabling Dual Boot...");

            var winPhoneBcdGuid = Guid.Parse("7619dcc9-fafe-11d9-b411-000476eba25f");

            var bootVolume = await GetBootVolume();
            await bootVolume.Partition.SetGptType(PartitionType.Esp);
            var bcdInvoker = new BcdInvoker((await GetEfiespVolume()).GetBcdFullFilename());
            bcdInvoker.Invoke($@"/displayorder {{{winPhoneBcdGuid}}} /remove");

            Log.Information("Dual Boot disabled");
        }

        private async Task<Partition> GetBootPartition()
        {
            var partitions = await disk.GetPartitions();
            var systemPartition = partitions.FirstOrDefault(x => Equals(x.PartitionType, PartitionType.Esp));
            return systemPartition;
        }
    }
}