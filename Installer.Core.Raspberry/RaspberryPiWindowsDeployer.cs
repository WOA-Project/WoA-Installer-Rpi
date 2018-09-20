using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Installer.Core;
using Installer.Core.Exceptions;
using Installer.Core.FileSystem;
using Installer.Core.Services;
using Installer.Core.Utils;
using Serilog;

namespace Installer.Raspberry.Core
{
    public class RaspberryPiWindowsDeployer : IWindowsDeployer<RaspberryPi>
    {
        private const string WindowsPartitonLabel = "WindowsARM";
        private static readonly string BcdBootPath = SystemPaths.BcdBoot;

        private readonly IWindowsImageService windowsImageService;
        private readonly DriverPaths driverPaths;

        public RaspberryPiWindowsDeployer(IWindowsImageService windowsImageService, DriverPaths driverPaths)
        {
            this.windowsImageService = windowsImageService;
            this.driverPaths = driverPaths;
        }

        public async Task Deploy(InstallOptions options, RaspberryPi phone, IObserver<double> progressObserver = null)
        {
            Log.Information("Deploying Windows 10 ARM64...");

            await RemoveExistingWindowsPartitions(phone);
            await CreateWindowsPartition(phone);

            var windowsVolumes = new WindowsVolumes(await phone.GetBootVolume(), await phone.GetWindowsVolume());
            await ApplyWindowsImage(windowsVolumes, options, progressObserver);
            await InjectDrivers(windowsVolumes.Windows);
            await MakeBootable(windowsVolumes, phone);

            Log.Information("Windows Image deployed");
        }

        private async Task MakeBootable(WindowsVolumes volumes, RaspberryPi phone)
        {
            Log.Information("Making Windows installation bootable...");

            var bcdPath = Path.Combine(volumes.Boot.RootDir.Name, "EFI", "Microsoft", "Boot", "BCD");
            var bcd = new BcdInvoker(bcdPath);
            var windowsPath = Path.Combine(volumes.Windows.RootDir.Name, "Windows");
            var bootDriveLetter = volumes.Boot.Letter;

            if (!bootDriveLetter.HasValue)
            {
                throw new DeploymentException("The Boot volume letter isn't accessible");
            }

            await ProcessUtils.RunProcessAsync(BcdBootPath, $@"{windowsPath} /f UEFI /s {bootDriveLetter}:");
            bcd.Invoke("/set {default} testsigning on");
            bcd.Invoke("/set {default} nointegritychecks on");
            await volumes.Boot.Partition.SetGptType(PartitionType.Esp);
            var updatedBootVolume = await phone.GetBootVolume();
            Log.Verbose("Updated Boot Volume: {@Volume}", new { updatedBootVolume.Label, updatedBootVolume.Partition, });
            if (!Equals(updatedBootVolume.Partition.PartitionType, PartitionType.Esp))
            {
                Log.Warning("The system partition should be {Esp}, but it's {ActualType}", PartitionType.Esp, updatedBootVolume.Partition.PartitionType);
            }
        }

        private Task RemoveExistingWindowsPartitions(Device phone)
        {
            Log.Information("Cleaning existing Windows 10 ARM64 partitions...");

            return phone.RemoveExistingWindowsPartitions();
        }

        private Task InjectDrivers(Volume windowsVolume)
        {
            Log.Information("Injecting Drivers...");
            return windowsImageService.InjectDrivers(driverPaths.PreOobe, windowsVolume);
        }

        private async Task ApplyWindowsImage(WindowsVolumes volumes, InstallOptions options, IObserver<double> progressObserver = null)
        {
            Log.Information("Applying Windows Image...");
            await windowsImageService.ApplyImage(volumes.Windows, options.ImagePath, options.ImageIndex, progressObserver);
            progressObserver?.OnNext(double.NaN);
        }

        private async Task CreateWindowsPartition(RaspberryPi phone)
        {
            Log.Information("Creating Windows partition...");

            var windowsPartition = await phone.Disk.CreatePartition(ulong.MaxValue);
            var winVolume = await windowsPartition.GetVolume();
            await winVolume.Mount();
            await winVolume.Format(FileSystemFormat.Ntfs, WindowsPartitonLabel);

            Log.Information("Windows Partition created successfully");            
        }

        public async Task InjectPostOobeDrivers(RaspberryPi phone)
        {
            Log.Information("Injection of 'Post Windows Setup' drivers...");

            if (!Directory.Exists(driverPaths.PostOobe))
            {
                throw new DirectoryNotFoundException("There Post-OOBE folder doesn't exist");
            }

            if (Directory.GetFiles(driverPaths.PostOobe, "*.inf").Any())
            {
                throw new InvalidOperationException("There are no drivers inside the Post-OOBE folder");
            }

            Log.Information("Checking Windows Setup status...");
            var isWindowsInstalled = await phone.IsOobeFinished();

            if (!isWindowsInstalled)
            {
                throw new InvalidOperationException(Resources.DriversInjectionWindowsNotFullyInstalled);
            }

            Log.Information("Injecting 'Post Windows Setup' Drivers...");
            var windowsVolume = await phone.GetWindowsVolume();

            await windowsImageService.InjectDrivers(driverPaths.PostOobe, windowsVolume);

            Log.Information("Drivers installed successfully");
        }

        public Task<bool> AreDeploymentFilesValid()
        {
            var pathsToCheck = new[] { driverPaths.PreOobe };
            var areValid = pathsToCheck.EnsureExistingPaths();
            return Task.FromResult(areValid);
        }
    }
}
