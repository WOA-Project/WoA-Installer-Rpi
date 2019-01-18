using System;
using System.ComponentModel;
using Installer.Lumia.ViewModels;
using Serilog;
using Serilog.Events;

namespace Installer.Lumia.Application.Views
{
    public partial class MainWindow
    {
        private readonly MainViewModel mainViewModel;

        public MainWindow()
        {
            InitializeComponent();

            IObservable<LogEvent> logEvents = null;

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Observers(x => logEvents = x, LogEventLevel.Information)
                .WriteTo.RollingFile(@"Logs\{Date}.txt")
                .CreateLogger();

            mainViewModel = CompositionRoot.GetMainViewModel(logEvents);
            DataContext = mainViewModel;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            mainViewModel.Dispose();

            base.OnClosing(e);
        }
    }
}
