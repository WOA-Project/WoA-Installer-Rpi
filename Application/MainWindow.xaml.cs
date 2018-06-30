using System;
using Serilog;

namespace Intaller.Wpf
{
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            var config = 
            this.DataContext = new MainViewModel();

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Observers(events => events.Subscribe(x => LoggingTextBox.Text +=  x.RenderMessage() + Environment.NewLine))
                .CreateLogger();            
        }
    }
}
