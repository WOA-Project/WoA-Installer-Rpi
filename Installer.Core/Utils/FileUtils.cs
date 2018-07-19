using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace Installer.Core.Utils
{
    public static class FileUtils
    {
        public static bool EnsureExistingPaths(this string[] pathsToCheck)
        {
            return pathsToCheck.All(IsExistingPath);
        }

        public static void EnsureEmptyDirectory(string path)
        {
            Log.Verbose("Ensuring that '{Directory}' is empty", path);
            
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }

        public static async Task Copy(string source, string destination, CancellationToken cancellationToken)
        {
            var fileOptions = FileOptions.Asynchronous | FileOptions.SequentialScan;
            var bufferSize = 4096;

            using (var sourceStream =
                new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, fileOptions))

            using (var destinationStream =
                new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None, bufferSize, fileOptions))

                await sourceStream.CopyToAsync(destinationStream, bufferSize, cancellationToken).ConfigureAwait(false);
        }

        public static async Task Copy(string source, string destination, FileMode fileMode = FileMode.Create)
        {
            Log.Verbose("Copying file {Source} to {Destination}", source, destination);
            
            var fileOptions = FileOptions.Asynchronous | FileOptions.SequentialScan;
            var bufferSize = 4096;

            using (var sourceStream =
                new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, fileOptions))

            using (var destinationStream =
                new FileStream(destination, fileMode, FileAccess.Write, FileShare.None, bufferSize, fileOptions))

                await sourceStream.CopyToAsync(destinationStream, bufferSize).ConfigureAwait(false);
        }

        public static async Task CopyDirectory(DirectoryInfo source, DirectoryInfo destination)
        {
            Log.Verbose("Copying directory {Source} to {Destination}", source, destination);


            foreach (var dir in source.GetDirectories())
            {
                await CopyDirectory(dir, destination.CreateSubdirectory(dir.Name));
            }

            foreach (var file in source.GetFiles())
            {
                var destFileName = Path.Combine(destination.FullName, file.Name);
                await Copy(file.FullName, destFileName);
            }
        }

        private static bool IsExistingPath(string path)
        {
            return File.Exists(path) || Directory.Exists(path);
        }
    }
}