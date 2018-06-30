using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Installer.Core.FullFx;
using Intaller.Wpf;
using Xunit;

namespace Application.Tests
{
    
    public class LowLevelApiSpecs
    {
        [Fact]
        public async Task GetPhoneDisk()
        {
            var sut = new LowLevelApi();
            var disk = await sut.GetPhoneDisk();
            Assert.NotNull(disk);
        }

        [Fact]
        public async Task EnsurePartitionMounted()
        {
            var sut = new LowLevelApi();
            await sut.EnsurePartitionMounted("EFIESP", "FAT");
        }

        [Fact]
        public async Task GetAvailableFreeSpace()
        {
            var sut = new LowLevelApi();
            var phoneDisk = await sut.GetPhoneDisk();
            var free = await sut.GetAvailableFreeSpace(phoneDisk);
            Assert.True(free > 0);
        }

        [Fact]
        public async Task RemoveExistingWindowsPartitions()
        {
            var sut = new LowLevelApi();
            await sut.RemoveExistingWindowsPartitions();
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
            var partition = (await sut.GetPartitions(await sut.GetPhoneDisk())).First();
            var volume = await sut.GetVolume(partition);
            Assert.NotNull(volume);
        }

        [Fact]
        public async Task ResizePartition()
        {
            var sut = new LowLevelApi();
            var partitions = await sut.GetPartitions(await sut.GetPhoneDisk());
            var volumes = await partitions.ToObservable().SelectMany(x => sut.GetVolume(x)).ToList();
            var volumeToResize = volumes.First(x => x.Label == "Data");
            var sizeInBytes = (int)(0.5 * 1_000_000_000);
            await sut.ResizePartition(volumeToResize.Partition, sizeInBytes);
        }
    }
}
