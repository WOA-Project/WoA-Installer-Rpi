using System;
using System.Threading.Tasks;

namespace Installer.Core
{
    public class WindowsDeployment
    {
        private readonly ILowLevelApi lowLevelApi;
        private readonly Config config;
        private int spaceNeeeded = 1 * 1_000_000_000;

        public WindowsDeployment(ILowLevelApi lowLevelApi, Config config)
        {
            this.lowLevelApi = lowLevelApi;
            this.config = config;
        }

        public async Task Execute()
        {
            await RemoveExistingWindowsPartitions();
            await AllocateSpace();
            await CreatePartitions();
            await DeployWindows();
            await InjectBasicDrivers();            
        }

        private Task RemoveExistingWindowsPartitions()
        {
            return lowLevelApi.RemoveExistingWindowsPartitions();
        }

        private Task InjectBasicDrivers()
        {
            throw new NotImplementedException();
        }

        private Task DeployWindows()
        {
            throw new NotImplementedException();
        }

        private Task CreatePartitions()
        {
            throw new NotImplementedException();
        }

        private async Task AllocateSpace()
        {
            var available = await lowLevelApi.GetAvailableFreeSpace(config.PhoneDisk);
            if (available < spaceNeeeded)
            {
                await TakeSpaceFromDataPartition();
            }
        }

        private async Task TakeSpaceFromDataPartition()
        {
            var finalSize = config.DataVolume.Size - spaceNeeeded;
            await lowLevelApi.ResizePartition(config.DataVolume.Partition, finalSize);
        }
    }
}