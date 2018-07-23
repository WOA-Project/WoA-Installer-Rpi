using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Installer.Core;
using Installer.Core.FullFx;
using Installer.Core.Services;
using Installer.Lumia.Core;
using Installer.Lumia.ViewModels;
using Installer.UI;
using Installer.ViewModels.Core;
using Installer.Wpf.Core.Services;
using Intaller.Wpf.Views;
using MahApps.Metro.Controls.Dialogs;
using Serilog.Events;

namespace Intaller.Wpf
{
    public static class CompositionRoot 
    {
        public static MainViewModel GetMainViewModel(IObservable<LogEvent> logEvents)
        {
            IDictionary<PhoneModel, IDeployer<Phone>> deployerDict = new Dictionary<PhoneModel, IDeployer<Phone>>
            {
                {PhoneModel.Lumia950Xl, GetDeployer(Path.Combine("Files", "Lumia 950 XL"))},
                {PhoneModel.Lumia950, GetDeployer(Path.Combine("Files", "Lumia 950"))},
            };

            var api = new LowLevelApi();
            var deployersItems = deployerDict.Select(pair => new DeployerItem(pair.Key, pair.Value)).ToList();
            var viewService = new ViewService();
            viewService.Register("TextViewer", typeof(TextViewerWindow));
            var mainViewModel = new MainViewModel(logEvents, deployersItems, new PackageImporterFactory(), new UIServices(new FilePicker(), viewService, new DialogService(DialogCoordinator.Instance)), new SettingsService(), async () => new Phone(await api.GetPhoneDisk()));
            return mainViewModel;
        }

        private static LumiaDeployer GetDeployer(string root)
        {
            return new LumiaDeployer(new LumiaCoreDeployer(root), new LumiaWindowsDeployer(new DismImageService(), new DriverPaths(root)) );
        }
    }  
}