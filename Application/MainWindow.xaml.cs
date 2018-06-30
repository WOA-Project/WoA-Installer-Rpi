using System;
using System.Windows;
using Serilog;

namespace Install
{
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            var config = 
            this.DataContext = new MainViewModel(new Setup());

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Observers(events => events.Subscribe(x => LoggingTextBox.Text +=  x.RenderMessage() + Environment.NewLine))
                .CreateLogger();            
        }
    }
}
