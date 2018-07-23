using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Installer.Core.Utils;
using Serilog;
using SharpCompress.Archives;
using SharpCompress.Common;
using ExtractionException = Installer.Core.Exceptions.ExtractionException;

namespace Installer.Core.Services
{
    public class ArchiveUncompressor<TEntry, TVolume, TArchive> : IArchiveUncompressor
        where TEntry : IArchiveEntry
        where TVolume : IVolume
        where TArchive : AbstractArchive<TEntry, TVolume>
    {
        private readonly Func<string, TArchive> getArchive;

        public ArchiveUncompressor(Func<string, TArchive> getArchive)
        {
            this.getArchive = getArchive;
        }

        public async Task Extract(string archivePath, string destination, IObserver<double> progressObserver = null)
        {
            Log.Information("Extracting from {File}", archivePath);

            using (var package = getArchive(archivePath))
            {
                var entries = package.Entries.Where(x => !x.IsDirectory).ToList();

                FileUtils.DeleteDirectyRecursive(destination);
                CreateDestinationDirectories(entries, destination);
                await Extract(entries.ToList(), destination, progressObserver);
            }

            Log.Information("Extraction successful from {Path}", archivePath);
        }

        public async Task<string> ReadToEnd(string archivePath, string key)
        {
            Log.Information("Importing Core Package from {File}", archivePath);

            using (var package = getArchive(archivePath))
            {
                var entry = package.Entries.SingleOrDefault(x => !x.IsDirectory && x.Key.StartsWith(key, StringComparison.InvariantCultureIgnoreCase));
                if (entry == null)
                {
                    return null;
                }

                using (var reader = new StreamReader(entry.OpenEntryStream()))
                {
                    var text = await reader.ReadToEndAsync();
                    Log.Information("Entry read");
                    return text;
                }                
            }            
        }

        private void CreateDestinationDirectories(IEnumerable<TEntry> entries, string destination)
        {
            var directories = entries.Select(x => Path.GetDirectoryName(x.Key.Replace("/", "\\"))).Distinct();
            foreach (var path in directories)
            {
                Directory.CreateDirectory(Path.Combine(destination, path));
            }
        }


        private static async Task Extract(ICollection<TEntry> entries, string destination, IObserver<double> progressObserver = null)
        {
            var count = entries.Count;
            double i = 0;

            var obs = entries
                .ToObservable()
                .Select(x => Observable.FromAsync(() => ExtractEntryToFile(x, destination)))
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

        private static async Task ExtractEntryToFile(TEntry pe, string destination)
        {
            using (var input = pe.OpenEntryStream())
            {
                var replacedPath = pe.Key.Replace("/", "\\");
                var finalPath = Path.Combine(destination, replacedPath);
                using (var output = File.OpenWrite(finalPath))
                {
                    await input.CopyToAsync(output);
                }
            }
        }
    }
}