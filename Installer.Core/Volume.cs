using System;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Serilog;

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

        public ILowLevelApi LowLevelApi => Partition.LowLevelApi;

        public async Task Mount()
        {
            Log.Verbose("Mounting volume {Volume}", this);
            var driveLetter = await LowLevelApi.GetFreeDriveLetter();
            await LowLevelApi.AssignDriveLetter(this, driveLetter);

            await Observable.Defer(() => Observable.Return(UpdateLetter(driveLetter))).RetryWithBackoffStrategy();
        }

        private Unit UpdateLetter(char driveLetter)
        {
            try
            {
                rootDir = new DirectoryInfo($"{driveLetter}:");
                return Unit.Default;
            }
            catch (Exception)
            {
                Log.Verbose("Cannot get path for drive letter {DriveLetter} while mounting partition {Partition}", driveLetter, this);
                throw;
            }
        }

        public override string ToString()
        {
            return $"{nameof(Label)}: '{Label}', {nameof(Partition)}: {Partition}, {nameof(Letter)}: {Letter}";
        }
    }
}