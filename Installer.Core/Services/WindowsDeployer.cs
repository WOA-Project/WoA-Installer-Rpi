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
        private static readonly ByteSize SpaceNeededForWindows = ByteSize.FromGigaBytes(19);
        private static readonly ByteSize ReservedPartitionSize = ByteSize.FromMegaBytes(200);
        private static readonly ByteSize BootPartitionSize = ByteSize.FromMegaBytes(100);
        private const string BootPartitionLabel = "BOOT";
        private const string WindowsPartitonLabel = "WindowsARM";
        private const string BcdBootPath = @"c:\Windows\SysNative\bcdboot.exe";
        private readonly Phone phone;
        private readonly IWindowsImageService windowsImageService;


        public WindowsDeployer(IWindowsImageService windowsImageService, Phone phone)
        {
            this.windowsImageService = windowsImageService;
            this.phone = phone;
        }

        public async Task Deploy(string imagePath, int imageIndex = 1, IObserver<double> progressObserver = null)
        {
            Log.Information("Deploying Windows 10 ARM64...");

            await RemoveExistingWindowsPartitions();
            await AllocateSpace();
            var partitions = await CreatePartitions();

            await ApplyWindowsImage(partitions, imagePath, imageIndex, progressObserver);
            await InjectBasicDrivers(partitions.Windows);
            await MakeBootable(partitions);

            Log.Information("Windows Image deployed");
        }

        public async Task MakeBootable(WindowsVolumes volumes)
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
        }

        private Task RemoveExistingWindowsPartitions()
        {
            Log.Information("Cleaning existing Windows 10 ARM64 partitions...");

            return phone.RemoveExistingWindowsPartitions();
        }

        private Task InjectBasicDrivers(Volume windowsVolume)
        {
            Log.Information("Injecting Basic Drivers...");
            return windowsImageService.InjectDrivers(FileSystemPaths.DriversPath, windowsVolume);
        }

        private async Task ApplyWindowsImage(WindowsVolumes volumes, string imagePath, int imageIndex = 1,
            IObserver<double> progressObserver = null)
        {
            Log.Information("Applying Windows Image...");
            await windowsImageService.ApplyImage(volumes.Windows, imagePath, imageIndex, progressObserver);
            progressObserver?.OnNext(double.NaN);
        }

        private async Task<WindowsVolumes> CreatePartitions()
        {
            Log.Information("Creating Windows partitions...");

            await phone.Disk.CreateReservedPartition((ulong) ReservedPartitionSize.Bytes);

            var bootPartition = await phone.Disk.CreatePartition((ulong) BootPartitionSize.Bytes);
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

        private async Task AllocateSpace()
        {
            Log.Information("Verifying the available space...");

            var refreshedDisk = await phone.Disk.LowLevelApi.GetPhoneDisk();
            var available = refreshedDisk.Size - refreshedDisk.AllocatedSize;

            if (available < SpaceNeededForWindows)
            {
                Log.Warning("There's not enough space in the phone");
                await TakeSpaceFromDataPartition();
                Log.Information("Data partition resized correctly");
            }
        }

        private async Task TakeSpaceFromDataPartition()
        {
            var dataVolume = await phone.GetDataVolume();

            Log.Warning("We will try to resize the Data partition to get the required space...");
            var finalSize = dataVolume.Size - SpaceNeededForWindows.Bytes;
            await dataVolume.Partition.Resize((ulong) finalSize);
        }

        public async Task InjectPostOobeDrivers()
        {
            Log.Information("Injection of 'Post Windows Setup' drivers");

            if (!Directory.Exists(FileSystemPaths.PostOobeDriverLocation))
            {
                throw new DirectoryNotFoundException("There Post-OOBE folder doesn't exist");
            }

            if (Directory.GetFiles(FileSystemPaths.PostOobeDriverLocation, "*.inf").Any())
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

            await windowsImageService.InjectDrivers(FileSystemPaths.PostOobeDriverLocation, windowsVolume);

            Log.Information("Drivers installed successfully");
        }

        public class WindowsVolumes
        {
            public WindowsVolumes(Volume bootVolume, Volume windowsVolume)
            {
                Boot = bootVolume;
                Windows = windowsVolume;
            }

            public Volume Boot { get; }
            public Volume Windows { get; }
        }
    }
}