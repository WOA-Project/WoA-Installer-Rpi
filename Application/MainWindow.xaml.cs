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

            this.DataContext = new MainViewModel(new Setup(new LowLevelApi()));

            Log.Logger = new LoggerConfiguration()
                .WriteTo.Observers(events => events.Subscribe(x => LoggingTextBox.Text +=  x.RenderMessage() + Environment.NewLine))
                .CreateLogger();            
        }
    }
}
