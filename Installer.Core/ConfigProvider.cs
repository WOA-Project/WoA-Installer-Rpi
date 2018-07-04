using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Serilog;

namespace Installer.Core
{
    public class ConfigProvider : IConfigProvider
    {
        private readonly ILowLevelApi api;

        public ConfigProvider(ILowLevelApi api)
        {
            this.api = api;
        }

        public async Task<Config> Retrieve()
        {
            Log.Verbose("Trying to get all the drives in the system");
            
            Log.Verbose("Drives queried successfully");
            
            try
            {
                await api.EnsurePartitionMounted("EFIESP", "FAT");
                var efiespDrive = await Observable.Defer(() => Observable.Return(GetEfiespDrive())).RetryWithBackoffStrategy();
                
                await api.EnsurePartitionMounted("Data", "NTFS");
                var phoneDisk = await api.GetPhoneDisk();
                var dataVolume = await Observable.Defer(() => Observable.FromAsync(() => GetDataVolume(phoneDisk))).RetryWithBackoffStrategy();

                return new Config(efiespDrive, phoneDisk, dataVolume);
            }
            catch (InvalidOperationException)
            {
                throw new InvalidOperationException("Cannot access Phone partitions");
            }            
        }

        private async Task<Volume> GetDataVolume(Disk phoneDisk)
        {
            
            var volumes = await api.GetVolumes(phoneDisk);
            var dataVolume = volumes.First(x => x.Label == "Data");
            return dataVolume;
        }

        private static DriveInfo GetEfiespDrive()
        {
            var drives = DriveInfo.GetDrives();

            var efiespDrive = drives.First(x =>
            {
                var isReady = x.IsReady;
                if (!isReady)
                {
                    Log.Warning("Drive {Drive} is not ready", x);
                }

                return isReady && x.DriveFormat == "FAT" && x.VolumeLabel == "EFIESP";
            });

            return efiespDrive;
        }

        private async Task<IList<Volume>> GetVolumes()
        {
            var disk = await api.GetPhoneDisk();
            var partitions = await api.GetPartitions(disk);
            var selectMany = partitions.ToObservable()
                .Where(IsMountable)
                .Select(x => Observable.FromAsync(() => api.GetVolume(x)))
                .Merge(1);

            var volumes = await selectMany.ToList();
            return volumes;
        }

        private static bool IsMountable(Partition partition)
        {
            var reserved = PartitionType.Reserved;
            return partition.GptType.Equals(reserved.Guid);
        }
    }
}