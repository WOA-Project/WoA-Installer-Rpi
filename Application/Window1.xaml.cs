using System;
using Intaller.Wpf.UIServices;
using Serilog;
using Serilog.Events;

namespace Intaller.Wpf
{
    public partial class Window1
    {
        public Window1()
        {
            InitializeComponent();

            IObservable<LogEvent> events = null;

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Observers(x => events = x)                    
                .CreateLogger();   

            DataContext = new MainViewModel(events, new WpfOpenFileService());       
        }
    }
}
