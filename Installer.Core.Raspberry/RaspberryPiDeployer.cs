using System;
using System.IO;
using System.Threading.Tasks;
using Installer.Core;
using Installer.Core.Exceptions;
using Installer.Core.Services;
using Installer.Core.Utils;
using Serilog;

namespace Installer.Raspberry.Core
{
    public class RaspberryPiDeployer : IDeployer<RaspberryPi>
    {
        private readonly IImageFlasher flasher;
        private readonly IWindowsDeployer<RaspberryPi> windowsDeployer;

        public RaspberryPiDeployer(IImageFlasher flasher, IWindowsDeployer<RaspberryPi> windowsDeployer)
        {
            this.flasher = flasher;
            this.windowsDeployer = windowsDeployer;
        }

        public async Task DeployCoreAndWindows(InstallOptions options, RaspberryPi device, IObserver<double> progressObserver = null)
        {
            await EnsureValidCoreWindowsDeployment();

            await CreateInitialPartitionLayout(device, progressObserver);
            await DeployUefi(device);
            await DeployWindows(options, device, progressObserver);
        }

        private async Task EnsureValidCoreWindowsDeployment()
        {
            var windowsValid = await windowsDeployer.AreDeploymentFilesValid();

            var driverPaths = new[]
            {
                Path.Combine("Files", "Core"),
                Path.Combine("Files", "UEFI"),
                Path.Combine("Files", "Drivers"),
                Path.Combine("Files", "Tools"),
            };

            var coreValid = driverPaths.EnsureExistingPaths();

            var isValid = coreValid && windowsValid;
            if (!isValid)
            {
                throw new InvalidDeploymentRepositoryException("The Files repository doesn't contain the required files. Please, check that you've installed a valid Core Package");
            }            
        }

        private async Task CreateInitialPartitionLayout(RaspberryPi device, IObserver<double> progressObserver)
        {
            Log.Information("Flashing GPT image...");
            await flasher.Flash(device.Disk, @"Files\Core\gpt.zip", progressObserver);
            Log.Information("GPT image flashed");
        }

        private async Task DeployUefi(Device device)
        {
            var efiesp = await device.GetBootVolume();
            await FileUtils.CopyDirectory(new DirectoryInfo(Path.Combine("Files", "UEFI")), efiesp.RootDir);
        }

        public async Task DeployWindows(InstallOptions options, RaspberryPi device, IObserver<double> progressObserver = null)
        {
            await windowsDeployer.Deploy(options, device, progressObserver);
        }

        public Task InjectPostOobeDrivers(RaspberryPi device)
        {
            throw new NotImplementedException();
        }
    }
}