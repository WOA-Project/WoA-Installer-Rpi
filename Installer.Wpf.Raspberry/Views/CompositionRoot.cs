using System;
using Installer.Core.FullFx;
using Installer.Core.Services;
using Installer.Raspberry.Core;
using Installer.Raspberry.ViewModels;
using Installer.UI;
using Installer.ViewModels.Core;
using Installer.Wpf.Core.Services;
using MahApps.Metro.Controls.Dialogs;
using Serilog.Events;

namespace Installer.Wpf.Raspberry.Views
{
    public static class CompositionRoot
    {
        public static object GetMainViewModel(IObservable<LogEvent> logEvents)
        {
            var deployer = new RaspberryPiDeployer(new ImageFlasher(), new RaspberryPiWindowsDeployer(new DismImageService(), new DriverPaths(@"Files")));

            var lowLevelApi = new LowLevelApi();
            var diskService = new DiskService(lowLevelApi);
            var uiServices = new UIServices(new FilePicker(), new ViewService(), new DialogService(DialogCoordinator.Instance));
            return new MainViewModel(logEvents, deployer, new PackageImporterFactory(), diskService, uiServices, new SettingsService());
        }
    }
}