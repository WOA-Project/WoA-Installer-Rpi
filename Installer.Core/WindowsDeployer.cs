using System;
using System.IO;
using System.Threading.Tasks;
using Serilog;

namespace Installer.Core
{
    public class WindowsDeployer : IWindowsDeployer
    {
        private const ulong SpaceNeeded = 19 * (ulong) 1_000_000_000;
        private readonly IWindowsImageService windowsImageService;
        private readonly Phone phone;
        private BcdInvoker bcd;
        private const string DriverLocation = @"Files\Drivers\Pre-OOBE";
        private const string PostOobeDriverLocation = @"Files\Drivers\Post-OOBE";

        public WindowsDeployer(ILowLevelApi lowLevelApi,
            IWindowsImageService windowsImageService, Phone phone)
        {
            this.windowsImageService = windowsImageService;
            this.phone = phone;
        }

        public async Task Deploy(string imagePath, int imageIndex = 1, IObserver<double> progressObserver = null)
        {
            Log.Information("Deploying Windows ARM64...");

            await RemoveExistingWindowsPartitions();
            await AllocateSpace();
            var partitions = await CreatePartitions();
            
            await DeployWindows(partitions, imagePath, imageIndex, progressObserver);
            await InjectBasicDrivers(partitions.Windows);
            await MakeBootable(partitions);

            Log.Information("Windows Image deployed");
        }

        public async Task MakeBootable(WindowsVolumes volumes)
        {
            Log.Information("Making Windows installation bootable...");

            var bcdPath = Path.Combine(volumes.Boot.RootDir.Name, "EFI", "Microsoft", "Boot", "BCD");
            bcd = new BcdInvoker(bcdPath);
            await CmdUtils.RunProcessAsync(@"c:\Windows\SysNative\bcdboot.exe", $@"{Path.Combine(volumes.Windows.RootDir.Name, "Windows")} /f UEFI /s {volumes.Boot.Letter}:");
            bcd.Invoke("/set {default} testsigning on");
            bcd.Invoke("/set {default} nointegritychecks on");
            await volumes.Boot.Partition.SetGptType(PartitionType.Esp);
        }

        private Task RemoveExistingWindowsPartitions()
        {
            Log.Information("Cleaning existing Windows ARM64 partitions...");

            return phone.RemoveExistingWindowsPartitions();
        }

        private Task InjectBasicDrivers(Volume windowsVolume)
        {
            Log.Information("Injecting Basic Drivers...");
            return windowsImageService.InjectDrivers(DriverLocation, windowsVolume);
        }

        private async Task DeployWindows(WindowsVolumes volumes, string imagePath, int imageIndex = 1, IObserver<double> progressObserver = null)
        {
            Log.Information("Applying Windows Image...");
            await windowsImageService.ApplyImage(volumes.Windows, imagePath, imageIndex, progressObserver);
            progressObserver?.OnNext(double.NaN);
        }

        private async Task<WindowsVolumes> CreatePartitions()
        {
            Log.Information("Creating Windows partitions...");

            await phone.Disk.CreateReservedPartition(200 * 1_000_000);

            var bootPartition = await phone.Disk.CreatePartition(100 * 1_000_000);
            var bootVolume = await bootPartition.GetVolume();
            await bootVolume.Mount();
            await bootVolume.Format(FileSystemFormat.Fat32, "BOOT");
            
            var windowsPartition = await phone.Disk.CreatePartition(ulong.MaxValue);
            var winVolume = await windowsPartition.GetVolume();
            await winVolume.Mount();
            await winVolume.Format(FileSystemFormat.Ntfs, "WindowsARM");
            
            return new WindowsVolumes(await phone.GetBootVolume(), await phone.GetWindowsVolume());
        }

        private async Task AllocateSpace()
        {
            Log.Information("Verifying the available space...");

            var refreshedDisk = await phone.Disk.LowLevelApi.GetPhoneDisk();
            var available = refreshedDisk.Size - refreshedDisk.AllocatedSize;

            if (available < SpaceNeeded)
            {
                Log.Warning("There's not enough space in the phone");
                await TakeSpaceFromDataPartition();
                Log.Information("Data partition resized");
            }
        }

        private async Task TakeSpaceFromDataPartition()
        {
            var dataVolume = await phone.GetDataVolume();

            Log.Warning("We will try to resize the Data partition to get the required space...");
            var finalSize = dataVolume.Size - SpaceNeeded;
            await dataVolume.Partition.Resize(finalSize);
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

        public async Task InjectPostOobeDrivers()
        {
            Log.Information("Injection of 'Post Windows Setup' drivers");

            Log.Information("Checking Windows Setup status...");
            var isWindowsInstalled = await phone.IsOobeFinished();

            if (!isWindowsInstalled)
            {
                throw new InvalidOperationException(Resources.DriversInjectionWindowsNotFullyInstalled);
            }

            Log.Information("Injecting 'Post Windows Setup' Drivers...");
            var windowsVolume = await phone.GetWindowsVolume();

            await windowsImageService.InjectDrivers(PostOobeDriverLocation, windowsVolume);

            Log.Information("Drivers installed successfully");
        }
    }
}