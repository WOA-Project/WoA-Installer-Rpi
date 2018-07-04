using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Installer.Core
{
    public interface ILowLevelApi
    {
        Task<Disk> GetPhoneDisk();
        Task EnsurePartitionMounted(string label, string filesystemType);
        Task RemoveExistingWindowsPartitions();
        Task<double> GetAvailableFreeSpace(Disk disk);
        Task ResizePartition(Partition partition, ulong sizeInBytes);
        Task<List<Partition>> GetPartitions(Disk disk);
        Task<Volume> GetVolume(Partition partition);
        Task<Partition> CreateReservedPartition(Disk disk, ulong sizeInBytes);
        Task<Partition> CreatePartition(Disk disk, ulong sizeInBytes);
        Task SetPartitionType(Partition partition, PartitionType partitionType);
        Task Format(Volume partition, FileSystemFormat ntfs, string fileSystemLabel);
        Task<char> GetFreeDriveLetter();
        Task AssignDriveLetter(Volume volume, char letter);
        Task<IList<Volume>> GetVolumes(Disk disk);
    }
}