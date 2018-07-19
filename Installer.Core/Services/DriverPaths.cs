using System.IO;

namespace Installer.Core.Services
{
    public class DriverPaths
    {
        private readonly string rootPath;

        public DriverPaths(string rootPath)
        {
            this.rootPath = rootPath;
        }
        
        public string PreOobe => Path.Combine(rootPath, "Drivers", "Pre-OOBE");
        public string PostOobe => Path.Combine(rootPath, "Drivers", "Post-OOBE");
    }
}