using System;
using Cinch.Reloaded.Services.Implementation;
using Installer.Core;
using Installer.Core.FullFx;
using Installer.Core.Services;
using MahApps.Metro.Controls.Dialogs;
using Serilog;
using Serilog.Events;

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

            var deployer = new Deployer(new LowLevelApi(), new DismImageService());
            DataContext = new MainViewModel(events, deployer, new DriverPackageImporter(), new WpfOpenFileService(), DialogCoordinator.Instance, visualizerService);
        }
    }
}
