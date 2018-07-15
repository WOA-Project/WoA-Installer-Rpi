using System;
using Serilog;
using Serilog.Events;

namespace Intaller.Wpf.Views
{
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            IObservable<LogEvent> logEvents = null;

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Observers(x => logEvents = x, LogEventLevel.Information)
                .WriteTo.RollingFile(@"Logs\{Date}.txt")
                .CreateLogger();

            DataContext = CompositionRoot.GetMainViewModel(logEvents);
        }
    }
}
