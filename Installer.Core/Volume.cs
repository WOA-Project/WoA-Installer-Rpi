using System.IO;
using System.Threading.Tasks;

namespace Installer.Core
{
    public class Volume
    {
        private DirectoryInfo rootDir;

        public Volume(Partition partition)
        {
            Partition = partition;
        }

        public string Label { get; set; }
        public ulong Size { get; set; }
        public Partition Partition { get; set; }
        public char? Letter { get; set; }

        public DirectoryInfo RootDir => rootDir ?? (rootDir = new DirectoryInfo($"{Letter}:"));

        public Task Format(FileSystemFormat ntfs, string fileSystemLabel)
        {
            return Partition.LowLevelApi.Format(this, ntfs, fileSystemLabel);
        }

        public Task AssignDriveLetter(Volume volume, char letter)
        {
            return LowLevelApi.AssignDriveLetter(volume, letter);
        }

        public ILowLevelApi LowLevelApi => Partition.LowLevelApi;
    }
}