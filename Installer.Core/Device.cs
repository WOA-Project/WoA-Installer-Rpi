using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Installer.Core.FileSystem;
using Registry;
using Serilog;

namespace Installer.Core
{
    public abstract class Device
    {
        protected Volume bootVolume;
        private Volume windowsVolume;

        protected Device(Disk disk)
        {
            Disk = disk;
        }

        public Disk Disk { get; }

        protected async Task<Volume> GetVolume(string label)
        {
            Log.Verbose("Getting {Label} volume", label);

            var volumes = await Disk.GetVolumes();

            var volume = volumes.SingleOrDefault(v => string.Equals(v.Label, label, StringComparison.InvariantCultureIgnoreCase));

            if (volume == null)
            {
                return null;
            }

            if (volume.Letter != null)
            {
                return volume;
            }

            Log.Verbose("{Label} volume wasn't mounted.", label);
            await volume.Mount();

            return volume;
        }

        public async Task<Volume> GetWindowsVolume()
        {
            return windowsVolume ?? (windowsVolume = await GetVolume("WindowsARM"));
        }

        protected async Task<bool> IsWoAPresent()
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

        public abstract Task<Volume> GetBootVolume();

        protected async Task<bool> IsWindowsPhonePresent()
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

        protected async Task<Partition> GetBootPartition()
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

        protected static async Task RemovePartition(string partitionName, Partition partition)
        {
            Log.Verbose("Trying to remove previously existing {Partition} partition", partitionName);
            if (partition != null)
            {
                Log.Verbose("{Partition} exists: Removing it...", partition);
                await partition.Remove();
                Log.Verbose("{Partition} removed", partition);
            }
        }

        public async Task<bool> IsOobeFinished()
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

        public abstract Task RemoveExistingWindowsPartitions();
    }
}