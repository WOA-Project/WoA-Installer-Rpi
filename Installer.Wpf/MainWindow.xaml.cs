using System;
using CinchExtended.Services.Implementation;
using Installer.Core;
using Installer.Core.FullFx;
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

            DataContext = new MainViewModel(events, new Setup(new LowLevelApi(), new DismImageService(), new DriverPackageImporter()),  new WpfOpenFileService(), DialogCoordinator.Instance, visualizerService);
        }
    }
}
