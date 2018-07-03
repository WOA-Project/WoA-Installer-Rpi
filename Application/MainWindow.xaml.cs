using System;
using System.Reactive.Linq;
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
                .WriteTo.Observers(x => events = x)                    
                .CreateLogger();   

            var config =  this.DataContext = new MainViewModel(events);

                    
        }
    }
}
