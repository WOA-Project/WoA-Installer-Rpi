using System.Threading.Tasks;
using ByteSizeLib;

namespace Installer.Core.FileSystem
{
    public class Partition
    {
        public Partition(Disk disk)
        {
            Disk = disk;
        }

        public Disk Disk { get; private set; }
        public uint Number { get; set; }
        public string Id { get; set; }
        public char Letter { get; set; }
        public PartitionType PartitionType { get; set; }
        public ILowLevelApi LowLevelApi => Disk.LowLevelApi;

        public override string ToString()
        {
            return $"{nameof(Disk)}: {Disk}, {nameof(Number)}: {Number}";
        }

        public async Task Resize(ByteSize sizeInBytes)
        {
            await LowLevelApi.ResizePartition(this, sizeInBytes);
        }

        public Task<Volume> GetVolume()
        {
            return LowLevelApi.GetVolume(this);
        }

        public Task SetGptType(PartitionType partitionType)
        {
            return LowLevelApi.SetPartitionType(this, partitionType);
        }

        public Task Remove()
        {
            return LowLevelApi.RemovePartition(this);
        }
    }
}