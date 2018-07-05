using System.Collections.Generic;
using System.Threading.Tasks;

namespace Installer.Core
{
    public class Disk
    {
        private readonly ILowLevelApi lowLevelApi;
        public uint Number { get; }
        public ulong Size { get; }
        public ulong AllocatedSize { get; }
        public ILowLevelApi LowLevelApi { get; set; }

        public Disk(ILowLevelApi lowLevelApi)
        {
            this.lowLevelApi = lowLevelApi;
        }

        public Disk(uint number, ulong size, ulong allocatedSize)
        {
            Number = number;
            Size = size;
            AllocatedSize = allocatedSize;
        }

        public Task<IList<Volume>> GetVolumes()
        {
            return lowLevelApi.GetVolumes(this);
        }

        public Task<List<Partition>> GetPartitions()
        {
            return lowLevelApi.GetPartitions(this);
        }

        public Task<Partition> CreatePartition(ulong sizeInBytes)
        {
            return lowLevelApi.CreatePartition(this, sizeInBytes);
        }

        public Task<Partition> CreateReservedPartition(ulong sizeInBytes)
        {
            return lowLevelApi.CreateReservedPartition(this, sizeInBytes);
        }
    }
}