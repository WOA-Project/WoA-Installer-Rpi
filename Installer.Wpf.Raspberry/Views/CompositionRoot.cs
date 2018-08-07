using System;
using Installer.Core;
using Installer.Core.FullFx;
using Installer.Core.Services;
using Installer.Raspberry.Core;
using Installer.Raspberry.ViewModels;
using Installer.UI;
using Installer.ViewModels.Core;
using Installer.Wpf.Core.Services;
using MahApps.Metro.Controls.Dialogs;
using Serilog.Events;

namespace Installer.Raspberry.Application.Views
{
    public static class CompositionRoot
    {
        public static object GetMainViewModel(IObservable<LogEvent> logEvents)
        {
            ServiceFactory.Current = new DefaultServiceFactory();

            var deployer = new RaspberryPiDeployer(new ImageFlasher(), new RaspberryPiWindowsDeployer(ServiceFactory.Current.ImageService, new DriverPaths(@"Files")));
            var uiServices = new UIServices(new FilePicker(), new ViewService(), new DialogService(DialogCoordinator.Instance));
            return new MainViewModel(logEvents, deployer, new PackageImporterFactory(), ServiceFactory.Current.DiskService, uiServices, new SettingsService());
        }
    }
}