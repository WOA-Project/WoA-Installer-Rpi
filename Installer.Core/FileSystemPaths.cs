using System.IO;

namespace Installer.Core
{
    public static class FileSystemPaths
    {
        public static readonly string DriversPath = Path.Combine("Files", "Drivers");
        public static readonly string DriverLocation = Path.Combine(DriversPath, "Pre-OOBE");
        public static readonly string PostOobeDriverLocation = Path.Combine(DriversPath, "Post-OOBE");
    }
}