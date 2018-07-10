using System.Collections.Generic;
using System.Threading.Tasks;

namespace Installer.Core.FileSystem
{
    public interface ILowLevelApi
    {
        Task<Disk> GetPhoneDisk();
        Task EnsurePartitionMounted(string label, string filesystemType);
        Task RemoveExistingWindowsPartitions();
        Task ResizePartition(Partition partition, ulong sizeInBytes);
        Task<List<Partition>> GetPartitions(Disk disk);
        Task<Volume> GetVolume(Partition partition);
        Task<Partition> CreateReservedPartition(Disk disk, ulong sizeInBytes);
        Task<Partition> CreatePartition(Disk disk, ulong sizeInBytes);
        Task SetPartitionType(Partition partition, PartitionType partitionType);
        Task Format(Volume volume, FileSystemFormat ntfs, string fileSystemLabel);
        Task<char> GetFreeDriveLetter();
        Task AssignDriveLetter(Volume volume, char letter);
        Task<IList<Volume>> GetVolumes(Disk disk);
        Task RemovePartition(Partition partition);
    }
}