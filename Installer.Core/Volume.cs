using System;
using System.IO;

namespace Installer.Core
{
    public class Volume
    {
        private DirectoryInfo rootDir;
        public string Label { get; set; }
        public ulong Size { get; set; }
        public Partition Partition { get; set; }
        public char? Letter { get; set; }

        public DirectoryInfo RootDir => rootDir ?? (rootDir = new DirectoryInfo($"{Letter}:"));
    }
}