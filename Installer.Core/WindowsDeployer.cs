using System;
using System.IO;
using System.Threading.Tasks;
using Serilog;

namespace Installer.Core
{
    public class WindowsDeployer : IWindowsDeployer
    {
        private readonly ILowLevelApi lowLevelApi;
        private readonly Config config;
        private readonly IWindowsImageService windowsImageService;
        private ulong spaceNeeded = (ulong)(0.5 * 1_000_000_000);

        public WindowsDeployer(ILowLevelApi lowLevelApi, Config config, IWindowsImageService windowsImageService)
        {
            this.lowLevelApi = lowLevelApi;
            this.config = config;
            this.windowsImageService = windowsImageService;
        }

        public async Task Deploy(string imagePath)
        {
            Log.Information("Deploying Windows ARM64...");

            await RemoveExistingWindowsPartitions();
            await AllocateSpace();
            var partitions = await CreatePartitions();
            await DeployWindows(partitions, imagePath);
            await InjectBasicDrivers(partitions.Windows);
            MakeBootable(partitions);
        }

        private async Task MakeBootable(WindowsPartitions partitions)
        {
            var volume = await lowLevelApi.GetVolume(partitions.Esp);

            CmdUtils.Run("bcdboot", $@"/l en-US /f UEFI /s {volume.Letter}:");

        }

        private Task RemoveExistingWindowsPartitions()
        {
            Log.Information("Removing existing partitions");

            return lowLevelApi.RemoveExistingWindowsPartitions();
        }

        private Task InjectBasicDrivers(Partition partitionsWindows)
        {
            throw new NotImplementedException();
        }

        private async Task DeployWindows(WindowsPartitions partitions, string imagePath)
        {
            await windowsImageService.ApplyImage(imagePath, partitions.Windows);
        }

        private Task<WindowsPartitions> CreatePartitions()
        {
            throw new NotImplementedException();
        }

        private async Task AllocateSpace()
        {
            Log.Information("Verifying the available space");

            var disk = config.PhoneDisk;
            var available = disk.Size - disk.AllocatedSize;

            if (available < spaceNeeded)
            {
                Log.Warning("Currently, there's not enough space in the phone");
                await TakeSpaceFromDataPartition();                
            }
            else
            {
                throw new NotEnoughSpaceException("Not enough space is available in the phone to install Windows");
            }
        }

        private async Task TakeSpaceFromDataPartition()
        {
            Log.Warning("Trying to resize Data partition");
            var finalSize = config.DataVolume.Size - spaceNeeded;
            await lowLevelApi.ResizePartition(config.DataVolume.Partition, finalSize);
        }

        private class WindowsPartitions
        {
            public Partition Esp { get; set; }
            public Partition Windows { get; set; }
        }
    }
}