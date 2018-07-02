using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Installer.Core
{
    public class ConfigStore
    {
        private readonly ILowLevelApi api;

        public ConfigStore(ILowLevelApi api)
        {
            this.api = api;
        }

        public async Task<Config> Retrieve()
        {
            var drives = DriveInfo.GetDrives();
            await api.EnsurePartitionMounted("EFIESP", "FAT");

            try
            {
                var efiespDrive = drives.First(x => x.DriveFormat == "FAT" && x.VolumeLabel == "EFIESP");
                return new Config(efiespDrive, await api.GetPhoneDisk(), (await GetVolumes()).First(x => x.Label == "Data"));
            }
            catch (InvalidOperationException e)
            {
                throw new InvalidOperationException("Cannot access Phone partitions");
            }            
        }

        private async Task<IList<Volume>> GetVolumes()
        {
            var partitions = await api.GetPartitions(await api.GetPhoneDisk());
            var selectMany = partitions.ToObservable()
                .Select(x => Observable.FromAsync(() => api.GetVolume(x)))
                .Merge(1);

            var volumes = await selectMany.ToList();
            return volumes;
        }
    }
}