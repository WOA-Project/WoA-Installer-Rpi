using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ByteSizeLib;
using Installer.Core;
using Installer.Core.Exceptions;
using Installer.Core.FileSystem;
using Installer.Core.Services;
using Serilog;

namespace Installer.Lumia.Core
{
    public class Phone : Device
    {
        private const string MainOsLabel = "MainOS";
        private static readonly ByteSize MinimumPhoneDiskSize = ByteSize.FromGigaBytes(28);
        private static readonly ByteSize MaximumPhoneDiskSize = ByteSize.FromGigaBytes(34);

        private static readonly Guid WinPhoneBcdGuid = Guid.Parse("7619dcc9-fafe-11d9-b411-000476eba25f");
        private Volume efiEspVolume;

        public Phone(Disk disk) : base(disk)
        {
        }

        public async Task<Volume> GetEfiespVolume()
        {
            return efiEspVolume ?? (efiEspVolume = await GetVolume("EFIESP"));
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
            var isOobeFinished = await IsOobeFinished();

            var bootPartition = await GetBootPartition();

            var isEnabled = bootPartition != null && Equals(bootPartition.PartitionType, PartitionType.Basic);

            var isCapable = isWoaPresent && isWPhonePresent && isOobeFinished;
            var status = new DualBootStatus(isCapable, isEnabled);

            Log.Information("Dual Boot Status retrieved");
            Log.Verbose("Dual Boot Status is {@Status}", status);

            return status;
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

            var bootVolume = await GetBootVolume();
            await bootVolume.Partition.SetGptType(PartitionType.Esp);
            var bcdInvoker = new BcdInvoker((await GetEfiespVolume()).GetBcdFullFilename());
            bcdInvoker.Invoke($@"/displayorder {{{WinPhoneBcdGuid}}} /remove");

            Log.Information("Dual Boot disabled");
        }

        public Task<Volume> GetDataVolume()
        {
            return GetVolume("Data");
        }

        public async Task RemoveWindowsPhoneBcdEntry()
        {
            Log.Verbose("Removing Windows Phone BCD entry...");

            var efiespVolume = await GetEfiespVolume();
            var bcdInvoker = new BcdInvoker(efiespVolume.GetBcdFullFilename());
            bcdInvoker.Invoke($@"/displayorder {{{WinPhoneBcdGuid}}} /remove");

            Log.Verbose("Windows Phone BCD entry removed");
        }

        public override async Task RemoveExistingWindowsPartitions()
        {
            Log.Information("Cleanup of possible previous Windows 10 ARM64 installation...");

            await RemovePartition("Reserved", await Disk.GetReservedPartition());
            await RemovePartition("WoA ESP", await GetBootPartition());
            var winVol = await GetWindowsVolume();
            await RemovePartition("WoA", winVol?.Partition);
        }

        public override async Task<Volume> GetBootVolume()
        {
            return await GetVolume("BOOT");
        }

        public static async Task<Phone> GetPhone()
        {
            var diskService = ServiceFactory.Current.DiskService;

            var disks = await diskService.GetDisks();
            foreach (var disk in disks)
            {
                var hasCorrectSize = HasCorrectSize(disk);

                if (hasCorrectSize)
                {
                    var volumes = await disk.GetVolumes();
                    var mainOs = volumes.FirstOrDefault(x => x.Label == MainOsLabel);
                    if (mainOs != null)
                    {
                        return new Phone(disk);
                    }
                }
            }

            throw new PhoneDiskNotFoundException(
                "Cannot get the Phone Disk. Please, verify that the Phone is in Mass Storage Mode.");
        }

        private static bool HasCorrectSize(Disk disk)
        {
            var moreThanMinimum = disk.Size > MinimumPhoneDiskSize;
            var lessThanMaximum = disk.Size < MaximumPhoneDiskSize;
            return moreThanMinimum && lessThanMaximum;
        }
    }
}