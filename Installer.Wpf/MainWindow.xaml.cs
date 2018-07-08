using System;
using Installer.Core;
using Installer.Core.FullFx;
using Intaller.Wpf.UIServices;
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

            DataContext = new MainViewModel(events, new Setup(new LowLevelApi(), new DismImageService()),  new WpfOpenFileService(), DialogCoordinator.Instance);
        }
    }
}
