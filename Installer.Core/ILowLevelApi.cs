using System.Collections.Generic;
using System.Threading.Tasks;

namespace Installer.Core
{
    public interface ILowLevelApi
    {
        Volume GetVolume(string label, string fileSystemFormat);
        Task<Disk> GetPhoneDisk();
        Task EnsurePartitionMounted(string label, string filesystemType);
        Task RemoveExistingWindowsPartitions();
        Task<double> GetAvailableFreeSpace(Disk disk);
        Task ResizePartition(Partition partition, long sizeInBytes);
        Task<List<Partition>> GetPartitions(Disk disk);
        Task<Volume> GetVolume(Partition partition);
    }
}