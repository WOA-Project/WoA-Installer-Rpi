using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ByteSizeLib;
using Installer.Core.FileSystem;
using Installer.Core.Utils;
using Serilog;

namespace Installer.Core.Services
{
    public class WindowsDeployer : IWindowsDeployer
    {
        private static readonly ByteSize SpaceNeededForWindows = ByteSize.FromGigaBytes(18);
        private static readonly ByteSize ReservedPartitionSize = ByteSize.FromMegaBytes(200);
        private static readonly ByteSize BootPartitionSize = ByteSize.FromMegaBytes(100);
        private const string BootPartitionLabel = "BOOT";
        private const string WindowsPartitonLabel = "WindowsARM";
        private const string BcdBootPath = @"c:\Windows\SysNative\bcdboot.exe";

        private readonly IWindowsImageService windowsImageService;
        private readonly DriverPaths driverPaths;


        public WindowsDeployer(IWindowsImageService windowsImageService, DriverPaths driverPaths)
        {
            this.windowsImageService = windowsImageService;
            this.driverPaths = driverPaths;
        }

        public async Task Deploy(InstallOptions options, Phone phone, IObserver<double> progressObserver = null)
        {
            Log.Information("Deploying Windows 10 ARM64...");

            await RemoveExistingWindowsPartitions(phone);
            await AllocateSpace(phone);
            var partitions = await CreatePartitions(phone);

            await ApplyWindowsImage(partitions, options, progressObserver);
            await InjectDrivers(partitions.Windows);
            await MakeBootable(partitions, phone);

            Log.Information("Windows Image deployed");
        }

        private async Task MakeBootable(WindowsVolumes volumes, Phone phone)
        {
            Log.Information("Making Windows installation bootable...");

            var bcdPath = Path.Combine(volumes.Boot.RootDir.Name, "EFI", "Microsoft", "Boot", "BCD");
            var bcd = new BcdInvoker(bcdPath);
            var windowsPath = Path.Combine(volumes.Windows.RootDir.Name, "Windows");
            var bootDriveLetter = volumes.Boot.Letter;
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

        private Task RemoveExistingWindowsPartitions(Phone phone)
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

        private async Task<WindowsVolumes> CreatePartitions(Phone phone)
        {
            Log.Information("Creating Windows partitions...");

            await phone.Disk.CreateReservedPartition((ulong)ReservedPartitionSize.Bytes);

            var bootPartition = await phone.Disk.CreatePartition((ulong)BootPartitionSize.Bytes);
            var bootVolume = await bootPartition.GetVolume();
            await bootVolume.Mount();
            await bootVolume.Format(FileSystemFormat.Fat32, BootPartitionLabel);

            var windowsPartition = await phone.Disk.CreatePartition(ulong.MaxValue);
            var winVolume = await windowsPartition.GetVolume();
            await winVolume.Mount();
            await winVolume.Format(FileSystemFormat.Ntfs, WindowsPartitonLabel);

            Log.Information("Windows Partitions created successfully");

            return new WindowsVolumes(await phone.GetBootVolume(), await phone.GetWindowsVolume());
        }

        private async Task AllocateSpace(Phone phone)
        {
            Log.Information("Verifying the available space...");

            var refreshedDisk = await phone.Disk.LowLevelApi.GetPhoneDisk();
            var available = refreshedDisk.Size - refreshedDisk.AllocatedSize;

            if (available < SpaceNeededForWindows)
            {
                Log.Warning("There's not enough space in the phone. Trying to take required space from the Data partition");

                await TakeSpaceFromDataPartition(phone);
                Log.Information("Data partition resized correctly");
            }
            else
            {
                Log.Verbose("We have enough available space to deploy Windows");
            }
        }

        private async Task TakeSpaceFromDataPartition(Phone phone)
        {
            Log.Information("Shrinking Data partition...");

            var dataVolume = await phone.GetDataVolume();
            var finalSize = dataVolume.Size - SpaceNeededForWindows;
            Log.Verbose("Resizing Data to {Size}", finalSize);

            await dataVolume.Partition.Resize(finalSize);
        }

        public async Task InjectPostOobeDrivers(Phone phone)
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