using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Installer.Core;
using Installer.Core.FileSystem;
using Installer.Core.FullFx;
using Installer.Core.Services;
using Serilog;
using Xunit;

namespace Application.Tests
{

    public class LowLevelApiSpecs
    {
        [Fact]
        public async Task GetDisks()
        {
            var sut = new LowLevelApi();
            var disk = await sut.GetDisks();
            Assert.NotNull(disk);
        }

        [Fact]
        public async Task GetPhoneDisk()
        {
            var sut = new LowLevelApi();
            var disk = await sut.GetPhoneDisk();
            Assert.NotNull(disk);
        }

        [Fact]
        public async Task Format()
        {
            var sut = new LowLevelApi();
            var phoneDisk = await sut.GetPhoneDisk();
            var partitionToFormat = (await sut.GetPartitions(phoneDisk)).Single(x => x.Number == 6);
            var toFormat = await sut.GetVolume(partitionToFormat);

            await sut.Format(toFormat, FileSystemFormat.Ntfs, "Test");
        }

        [Fact]
        public async Task AssignLetter()
        {
            var sut = new LowLevelApi();
            var phoneDisk = await sut.GetPhoneDisk();
            var partitionToFormat = (await sut.GetPartitions(phoneDisk)).Single(x => x.Number == 6);
            var toAssign = await sut.GetVolume(partitionToFormat);

            await sut.AssignDriveLetter(toAssign, 'I');
        }

        [Fact]
        public async Task DeployWindows()
        {
            var api = new LowLevelApi();
            var deployer = new WindowsDeployer(new DismImageService(), new DriverPaths(""));
            await deployer.Deploy(new InstallOptions(@"F:\sources\install.wim"), new Phone(null));
        }

        [Fact]
        public async Task SetPartitionType()
        {
            var sut = new LowLevelApi();
            var disk = await sut.GetPhoneDisk();
            var volumes = await sut.GetVolumes(disk);
            var volume = volumes.Single(x => x.Label == "BOOT");

            await sut.SetPartitionType(volume.Partition, PartitionType.Esp);
        }

        [Fact]
        public async Task GetPartitions()
        {
            var sut = new LowLevelApi();
            var partitions = await sut.GetPartitions(await sut.GetPhoneDisk());
            Assert.NotNull(partitions);
        }

        [Fact]
        public async Task GetVolume()
        {
            var sut = new LowLevelApi();
            var partition = (await sut.GetPartitions(await sut.GetPhoneDisk())).Skip(2).First();
            var volume = await sut.GetVolume(partition);
            Assert.NotNull(volume);
        }

        [Fact]
        public async Task GetAvailableLetter()
        {
            var sut = new LowLevelApi();
            var letter = await sut.GetFreeDriveLetter();
        }

        [Fact]
        public async Task ResizePartition()
        {
            var sut = new LowLevelApi();
            var partitions = await sut.GetPartitions(await sut.GetPhoneDisk());
            var volumes = await partitions.ToObservable()
                .Select(x => Observable.FromAsync(() => sut.GetVolume(x)))
                .Merge(1)
                .ToList();

            var volumeToResize = volumes.First(x => x.Label == "Data");
            var sizeInBytes = 2 * (ulong)1_000_000_000;
            await sut.ResizePartition(volumeToResize.Partition, sizeInBytes);
        }

        [Fact]
        public async Task CheckOobeCompleted()
        {
            var sut = new LowLevelApi();

            var volume = await sut.GetWindowsVolume();

            var completed = sut.GetIsOobeCompleted(volume);
        }
    }
}
