using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Install
{
    public static class FileUtils
    {
        public static async Task Copy(string sourceFile, string destinationFile, CancellationToken cancellationToken)
        {
            var fileOptions = FileOptions.Asynchronous | FileOptions.SequentialScan;
            var bufferSize = 4096;

            using (var sourceStream =
                new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, fileOptions))

            using (var destinationStream =
                new FileStream(destinationFile, FileMode.CreateNew, FileAccess.Write, FileShare.None, bufferSize, fileOptions))

                await sourceStream.CopyToAsync(destinationStream, bufferSize, cancellationToken)
                    .ConfigureAwait(continueOnCapturedContext: false);
        }

        public static async Task Copy(string sourceFile, string destinationFile, FileMode fileMode = FileMode.Create)
        {
            var fileOptions = FileOptions.Asynchronous | FileOptions.SequentialScan;
            var bufferSize = 4096;

            using (var sourceStream =
                new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, fileOptions))

            using (var destinationStream =
                new FileStream(destinationFile, fileMode, FileAccess.Write, FileShare.None, bufferSize, fileOptions))

                await sourceStream.CopyToAsync(destinationStream, bufferSize)
                    .ConfigureAwait(continueOnCapturedContext: false);
        }

        public static async Task CopyDirectory(DirectoryInfo source, DirectoryInfo target)
        {
            foreach (var dir in source.GetDirectories())
            {
                await CopyDirectory(dir, target.CreateSubdirectory(dir.Name));
            }

            foreach (var file in source.GetFiles())
            {
                var destFileName = Path.Combine(target.FullName, file.Name);
                await Copy(file.FullName, destFileName);
            }
        }

        
    }
}