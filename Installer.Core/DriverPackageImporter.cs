using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Serilog;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Common;

namespace Installer.Core
{
    public class DriverPackageImporter : IDriverPackageImporter
    {
        private const string RootFolder = "";
        private static readonly string ChangelogFilename = $"{RootFolder}Changelog.txt";

        public async Task ImportDriverPackage(string fileName, string destination, IObserver<double> progressObserver = null)
        {
            Log.Information("Importing Driver Package from {Filename}...", fileName);

            var pathMapping = new Dictionary<string, string>
            {
                {$"{RootFolder}Cityman/", Path.Combine(destination, "Pre-OOBE")},
                {$"{RootFolder}POSTOOBE/", Path.Combine(destination, "Post-OOBE")},
            };

            using (var package = SevenZipArchive.Open(fileName))
            {
                SanityCheck(package);

                var itemsToExtract = package.Entries.Where(entry => IsExtractable(entry, pathMapping)).Select(x => new PendingExtract() { Entry = x, Destination = DestinationFolder(x, pathMapping)})
                    .ToList();

                EnsureEmptyDestination(destination);
                await Extract(itemsToExtract, progressObserver);
            }

            Log.Information("Driver Package imported correctly", fileName);
        }

        private static void SanityCheck(SevenZipArchive archive)
        {
            if (!archive.Entries.Any(x => x.Key.StartsWith($"{RootFolder}Cityman", StringComparison.InvariantCultureIgnoreCase)))
            {
                throw new InvalidDriverPackageException("The driver package seems to be invalid");
            }
        }

        public async Task<string> GetReadmeText(string fileName)
        {
            var memoryStream = new MemoryStream();
            using (var package = SevenZipArchive.Open(fileName))
            {
                var readme = package.Entries.FirstOrDefault(x => string.Equals(x.Key, ChangelogFilename));
                if (readme == null)
                {
                    return null;
                }

                using (var input = readme.OpenEntryStream())
                {
                    await input.CopyToAsync(memoryStream);
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    var readToEnd = new StreamReader(memoryStream).ReadToEnd();
                    return readToEnd;
                }
            }            
        }

        private static void EnsureEmptyDestination(string destination)
        {
            if (Directory.Exists(destination))
            {
                Directory.Delete(destination, true);
            }
        }

        private static bool IsExtractable(IEntry entry, Dictionary<string, string> paths)
        {
            return !entry.IsDirectory && paths.Keys.Any(key => entry.Key.StartsWith(key, StringComparison.CurrentCultureIgnoreCase));
        }

        private static string DestinationFolder(IEntry sevenZipArchiveEntry, IDictionary<string, string> paths)
        {
            var key = paths.Keys.FirstOrDefault(x => sevenZipArchiveEntry.Key.StartsWith(x, StringComparison.CurrentCultureIgnoreCase));
            if (key == null)
            {
                throw new InvalidOperationException("They key cannot be null");
            }

            var destinationRoot = paths[key];
            var rightPart = new string(sevenZipArchiveEntry.Key.Skip(key.Length + 1).ToArray());
            var destinationFolder = Path.Combine(destinationRoot, rightPart);

            return destinationFolder.Replace("/", "\\");
        }

        private static async Task Extract(ICollection<PendingExtract> entries, IObserver<double> progressObserver = null)
        {
            var count = entries.Count;
            double i = 0;

            var obs = entries
                .ToObservable()
                .Select(x => Observable.FromAsync(() => ExtractEntryToFile(x)))
                .Merge(1)
                .Publish();

            var updater = obs.Subscribe(_ =>
            {
                progressObserver?.OnNext(i / count);
                i++;
            });

            obs.Connect();
            await obs.ToList();

            updater.Dispose();
           
            progressObserver?.OnNext(double.NaN);
        }

        private static async Task ExtractEntryToFile(PendingExtract pe)
        {
            using (var input = pe.Entry.OpenEntryStream())
            {
                Directory.CreateDirectory(Path.GetDirectoryName(pe.Destination) ?? throw new InvalidOperationException("The directory name should not be null"));
                using (var output = File.OpenWrite(pe.Destination))
                {
                    await input.CopyToAsync(output);
                }                                
            }
        }

        private class PendingExtract
        {
            public SevenZipArchiveEntry Entry { get; set; }
            public string Destination { get; set; }
        }
    }
}
