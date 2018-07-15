using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Installer.Core.Exceptions;
using Installer.Core.Utils;
using Serilog;
using SharpCompress.Archives;
using SharpCompress.Common;

namespace Installer.Core.Services
{
    public class DriverPackageImporter<TEntry, TVolume, TArchive> : IDriverPackageImporter 
        where TEntry: IArchiveEntry 
        where TVolume:IVolume
        where TArchive:AbstractArchive<TEntry, TVolume>
    {
        private readonly Func<string, TArchive> getArchive;
        private readonly List<(string, string)> pathMaps;
        private readonly string changelogFilename;

        public DriverPackageImporter(Func<string, TArchive> getArchive, string rootPath)
        {
            this.getArchive = getArchive;
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
            Log.Information("Importing Driver Package from {File}", packagePath);

            using (var package = getArchive(packagePath))
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

            Log.Information("Driver Package imported successfully", packagePath);
        }

        private void CreateDestinationDirectories(List<PendingEntry<TEntry>> paths)
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

        private IEnumerable<PendingEntry<TEntry>> GetPendingEntries(IEnumerable<TEntry> packageEntries, IEnumerable<(string, string)> valueTuples)
        {
            return valueTuples.SelectMany(x => GetEntriesFromMapping(packageEntries, x));
        }

        private IEnumerable<PendingEntry<TEntry>> GetEntriesFromMapping(IEnumerable<TEntry> packageEntries, (string, string) valueTuple)
        {
            var oldRootPath = valueTuple.Item1;

            var filteredEntries = packageEntries.Where(entry =>
                entry.Key.StartsWith(oldRootPath, StringComparison.InvariantCultureIgnoreCase));

            return filteredEntries.Select(x =>
            {
                var relative = new string(x.Key.Skip(oldRootPath.Length + 1).ToArray()).Replace("/", "\\");
                var destination = Path.Combine(valueTuple.Item2, relative);
                return new PendingEntry<TEntry>(x, destination);
            });
        }

        public async Task<string> GetReadmeText(string fileName)
        {
            var memoryStream = new MemoryStream();
            using (var package = getArchive(fileName))
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

        private static async Task Extract(ICollection<PendingEntry<TEntry>> entries, IObserver<double> progressObserver = null)
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

        private static async Task ExtractEntryToFile(PendingEntry<TEntry> pe)
        {
            using (var input = pe.Entry.OpenEntryStream())
            {            
                using (var output = File.OpenWrite(pe.Destination))
                {
                    await input.CopyToAsync(output);                    
                }
            }
        }

        private class PendingEntry<T> where T : IArchiveEntry
        {
            public PendingEntry(T entry, string destination)
            {
                Entry = entry;
                Destination = destination;
            }


            public T Entry { get; }
            public string Destination { get; }
        }
    }
}