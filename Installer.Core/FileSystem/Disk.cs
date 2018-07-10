using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Installer.Core.FileSystem
{
    public class Disk
    {
        public ILowLevelApi LowLevelApi { get; }
        public uint Number { get; }
        public ulong Size { get; }
        public ulong AllocatedSize { get; }

        public Disk(ILowLevelApi lowLevelApi, uint number, ulong size, ulong allocatedSize)
        {
            LowLevelApi = lowLevelApi;
            Number = number;
            Size = size;
            AllocatedSize = allocatedSize;
        }

        public async Task<IList<Volume>> GetVolumes()
        {
            var volumes = await LowLevelApi.GetVolumes(this);
            return volumes;
        }

        public Task<List<Partition>> GetPartitions()
        {
            return LowLevelApi.GetPartitions(this);
        }

        public Task<Partition> CreatePartition(ulong sizeInBytes)
        {
            return LowLevelApi.CreatePartition(this, sizeInBytes);
        }

        public Task<Partition> CreateReservedPartition(ulong sizeInBytes)
        {
            return LowLevelApi.CreateReservedPartition(this, sizeInBytes);
        }

        public async Task<Partition> GetReservedPartition()
        {
            var parts = await LowLevelApi.GetPartitions(this);
            return parts.FirstOrDefault(x => Equals(x.PartitionType, PartitionType.Reserved));
        }
    }
}