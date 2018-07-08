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
        private Volume boolVolume;
        private Volume efiEspVolume;
        private Volume windowsVolume;

        public Phone(Disk disk)
        {
            Disk = disk;
        }

        public Disk Disk { get; }

        private async Task<Volume> GetVolume(string label)
        {
            var volumes = await Disk.GetVolumes();
            var volume = volumes.FirstOrDefault(v => string.Equals(v.Label, label, StringComparison.InvariantCultureIgnoreCase));

            if (volume != null)
            {
                await volume.Mount();
            }

            return volume;
        }

        public async Task<Volume> GetEfiespVolume()
        {
            return efiEspVolume ?? (efiEspVolume = await GetVolume("EFIESP"));
        }

        public async Task<Volume> GetWindowsVolume()
        {
            return windowsVolume ?? (windowsVolume = await GetVolume("WindowsARM"));
        }

        public async Task<Volume> GetBootVolume()
        {
            return boolVolume ?? (boolVolume = await GetVolume("BOOT"));
        }

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
            var isOobeFinished = await IsObeeFinished();

            var bootPartition = await GetBootPartition();
            var isEnabled = Equals(bootPartition.PartitionType, PartitionType.Basic);

            var isCapable = isWoaPresent && isWPhonePresent && isOobeFinished;
            var status = new DualBootStatus(isCapable, isEnabled);


            Log.Information("Dual Boot Status retrieved");
            Log.Verbose("Dual Boot Status is {@Status}", status);

            return status;
        }

        private async Task<bool> IsObeeFinished()
        {
            var winVolume = await GetWindowsVolume();

            if (winVolume == null)
            {
                return false;
            }

            var path = Path.Combine(winVolume.RootDir.Name, "Windows", "System32", "Config", "System");
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
            var partitions = await Disk.GetPartitions();
            var bootPartition = partitions.FirstOrDefault(x => Equals(x.PartitionType, PartitionType.Esp));
            if (bootPartition != null)
            {
                return bootPartition;
            }

            var bootVolume = await GetBootVolume();
            return bootVolume?.Partition;
        }

        public Task<Volume> GetDataVolume()
        {
            return GetVolume("Data");
        }

        public async Task RemoveExistingWindowsPartitions()
        {
            Log.Information("Cleanup of possible previous Windows 10 ARM64 installation...");

            await RemovePartition("Reserved", await Disk.GetReservedPartition());
            await RemovePartition("WoA ESP", await GetBootPartition());
            var winVol = await GetWindowsVolume();
            await RemovePartition("WoA", winVol?.Partition);           
        }

        private static async Task RemovePartition(string partitionName, Partition partition)
        {
            Log.Verbose("Trying to remove previously existing {Partition} partition", partitionName);
            if (partition != null)
            {
                Log.Verbose("{Partition} exists: Removing it...");
                await partition.Remove();
                Log.Verbose("{Partition} removed");
            }
        }
    }
}