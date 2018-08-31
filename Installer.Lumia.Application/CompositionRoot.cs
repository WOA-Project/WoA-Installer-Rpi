using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Installer.Core;
using Installer.Core.FullFx;
using Installer.Core.Services;
using Installer.Lumia.Application.Views;
using Installer.Lumia.Core;
using Installer.Lumia.ViewModels;
using Installer.UI;
using Installer.ViewModels.Core;
using Installer.Wpf.Core.Services;
using MahApps.Metro.Controls.Dialogs;
using Serilog.Events;

namespace Installer.Lumia.Application
{
    public static class CompositionRoot
    {
        public static MainViewModel GetMainViewModel(IObservable<LogEvent> logEvents)
        {
            ServiceFactory.Current = new DefaultServiceFactory();

            IDictionary<PhoneModel, IDeployer<Phone>> deployerDict = new Dictionary<PhoneModel, IDeployer<Phone>>
            {
                {PhoneModel.Lumia950Xl, GetDeployer(Path.Combine("Files", "Cityman"))},
                {PhoneModel.Lumia950, GetDeployer(Path.Combine("Files", "Talkman"))}
            };

            var deployersItems = deployerDict.Select(pair => new DeployerItem(pair.Key, pair.Value)).ToList();
            var viewService = new ViewService();
            viewService.Register("TextViewer", typeof(TextViewerWindow));
            var mainViewModel = new MainViewModel(logEvents, deployersItems, new PackageImporterFactory(),
                new UIServices(new FilePicker(), viewService, new DialogService(DialogCoordinator.Instance)),
                new SettingsService(), Phone.GetPhone);
            return mainViewModel;
        }

        private static LumiaDeployer GetDeployer(string rootFilesPath)
        {
            return new LumiaDeployer(new LumiaCoreDeployer(rootFilesPath),
                new LumiaWindowsDeployer(ServiceFactory.Current.ImageService, new DriverPaths(rootFilesPath)));
        }
    }
}