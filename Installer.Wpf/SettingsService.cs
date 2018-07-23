using Installer.Lumia.ViewModels;
using Intaller.Wpf.Properties;

namespace Intaller.Wpf
{
    public class SettingsService : ISettingsService
    {
        public string DriverPackFolder
        {
            get => Settings.Default.DriverPackFolder;
            set => Settings.Default.DriverPackFolder = value;
        }

        public string WimFolder
        {
            get => Settings.Default.WimFolder;
            set => Settings.Default.WimFolder = value;
        }
    }
}