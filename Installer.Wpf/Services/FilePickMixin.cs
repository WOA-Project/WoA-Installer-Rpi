using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cinch.Reloaded.Services.Interfaces;
using Serilog;

namespace Intaller.Wpf.Services
{
    public static class FilePickMixin
    {
        public static string Pick(this IOpenFileService openFileService, IEnumerable<(string, IEnumerable<string>)> extensions, Func<string> getCurrentFolder, Action<string> setCurrentFolder)
        {
            var lines = extensions.Select(tuple =>
            {
                var exts = string.Join((string) ";", (IEnumerable<string>) tuple.Item2);
                return $"{tuple.Item1}|{exts}";
            });

            var filter = string.Join("|", lines);

            openFileService.Filter = filter;
            openFileService.FileName = "";
            openFileService.InitialDirectory = getCurrentFolder();
            if (openFileService.ShowDialog(null) == true)
            {
                var pickFileName = openFileService.FileName;
                var directoryName = Path.GetDirectoryName(pickFileName);
                setCurrentFolder(directoryName);
                Log.Verbose("Default directory for WimFolder has been set to {Folder}", directoryName);
                return pickFileName;
            }

            return null;
        }
    }
}