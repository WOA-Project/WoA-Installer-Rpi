using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Installer.Core.Exceptions;
using Installer.Core.Utils;
using SharpCompress.Archives.SevenZip;

namespace Installer.Core.Services
{
    public class BrandNewPackageImporter : IDriverPackageImporter
    {
        private readonly List<(string, string)> pathMaps;
        private readonly string changelogFilename;

        public BrandNewPackageImporter(string rootPath)
        {
            pathMaps = new List<(string, string)>
            {
                ("Cityman", Path.Combine(rootPath, "Lumia 950 XL", "Drivers", "Pre-OOBE")),
                ("PostOOBE", Path.Combine(rootPath, "Lumia 950 XL", "Drivers", "Post-OOBE")),
                ("Talkman", Path.Combine(rootPath, "Lumia 950", "Drivers", "Pre-OOBE")),
                ("PostOOBE", Path.Combine(rootPath, "Lumia 950", "Drivers", "Post-OOBE")),
            };

            changelogFilename = $"Changelog.txt";
        }

        public async Task ImportDriverPackage(string packagePath, string destination, IObserver<double> progressObserver = null)
        {
            using (var package = SevenZipArchive.Open(packagePath))
            {
                var files = package.Entries.Where(x => !x.IsDirectory);

                var entries = GetPendingEntries(files, pathMaps).ToList();

                if (!entries.Any())
                {
                    throw new InvalidDriverPackageException("The driver package seems to be invalid.");
                }

                EnsureEmptyPaths(pathMaps.Select(tuple => tuple.Item2));
                CreateDestinationDirectories(entries);
                await Extract(entries, progressObserver);
            }
        }

        private void CreateDestinationDirectories(List<PendingEntry> paths)
        {
            var directories = paths.Select(x => Path.GetDirectoryName(x.Destination)).Distinct();
            foreach (var path in directories)
            {
                Directory.CreateDirectory(path);
            }
        }

        private void EnsureEmptyPaths(IEnumerable<string> paths)
        {
            foreach (var path in paths)
            {
                FileUtils.EnsureEmptyDirectory(path);
            }
        }

        private static async Task Extract(ICollection<PendingEntry> entries, IObserver<double> progressObserver = null)
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

        private static async Task ExtractEntryToFile(PendingEntry pe)
        {
            using (var input = pe.Entry.OpenEntryStream())
            {            
                using (var output = File.OpenWrite(pe.Destination))
                {
                    await input.CopyToAsync(output);                    
                }
            }
        }

        private IEnumerable<PendingEntry> GetPendingEntries(IEnumerable<SevenZipArchiveEntry> packageEntries, IEnumerable<(string, string)> valueTuples)
        {
            return valueTuples.SelectMany(x => GetEntriesFromMapping(packageEntries, x));
        }

        private IEnumerable<PendingEntry> GetEntriesFromMapping(IEnumerable<SevenZipArchiveEntry> packageEntries, (string, string) valueTuple)
        {
            var oldRootPath = valueTuple.Item1;

            var filteredEntries = packageEntries.Where(entry =>
                entry.Key.StartsWith(oldRootPath, StringComparison.InvariantCultureIgnoreCase));

            return filteredEntries.Select(x =>
            {
                var relative = new string(x.Key.Skip(oldRootPath.Length + 1).ToArray()).Replace("/", "\\");
                var destination = Path.Combine(valueTuple.Item2, relative);
                return new PendingEntry(x, destination);
            });
        }

        public async Task<string> GetReadmeText(string fileName)
        {
            var memoryStream = new MemoryStream();
            using (var package = SevenZipArchive.Open(fileName))
            {
                var readme = package.Entries.FirstOrDefault(x => string.Equals(x.Key, changelogFilename));
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

        private class PendingEntry
        {
            public PendingEntry(SevenZipArchiveEntry entry, string destination)
            {
                Entry = entry;
                Destination = destination;
            }


            public SevenZipArchiveEntry Entry { get; }
            public string Destination { get; }
        }
    }
}