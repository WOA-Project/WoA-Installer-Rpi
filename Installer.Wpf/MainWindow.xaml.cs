using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cinch.Reloaded.Services.Implementation;
using Installer.Core;
using Installer.Core.FullFx;
using Installer.Core.Services;
using MahApps.Metro.Controls.Dialogs;
using Serilog;
using Serilog.Events;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Archives.Zip;
using SharpCompress.Common.SevenZip;
using SharpCompress.Common.Zip;

namespace Intaller.Wpf
{
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            IObservable<LogEvent> events = null;

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Observers(x => events = x, LogEventLevel.Information)
                .WriteTo.RollingFile(@"Logs\{Date}.txt")
                .CreateLogger();

            var visualizerService = new ExtendedWpfUIVisualizerService();
            visualizerService.Register("TextViewer", typeof(TextViewerWindow));

            IDictionary<PhoneModel, IDeployer> deployerDict = new Dictionary<PhoneModel, IDeployer>()
            {
                {PhoneModel.Lumia950Xl, GetDeployer(Path.Combine("Files", "Lumia 950 XL")) },
                {PhoneModel.Lumia950, GetDeployer(Path.Combine("Files", "Lumia 950")) },
            };

            var api = new LowLevelApi();
            var deployersItems = deployerDict.Select(pair => new DeployerItem(pair.Key, pair.Value)).ToList();
            var importerItems = new List<DriverPackageImporterItem>()
            {
                new DriverPackageImporterItem("7z", new SharpCompressImporter<SevenZipArchiveEntry, SevenZipVolume, SevenZipArchive>(s => SevenZipArchive.Open(s), "Files")) ,
                new DriverPackageImporterItem("zip", new SharpCompressImporter<ZipArchiveEntry, ZipVolume, ZipArchive>(s => ZipArchive.Open(s), "Files")) 
            };

            DataContext = new MainViewModel(events, deployersItems, importerItems, new BrandNewPackageImporter("Files"), new WpfOpenFileService(), DialogCoordinator.Instance, visualizerService, async () => new Phone(await api.GetPhoneDisk()));
        }

        private static Deployer GetDeployer(string root)
        {
            return new Deployer(new CoreDeployer(root), new WindowsDeployer(new DismImageService(), new DriverPaths(root)) );
        }
    }
}
