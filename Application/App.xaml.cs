using System;
using System.Reactive.Linq;
using System.Windows;
using Serilog;

namespace Install
{
    /// <summary>
    /// Lógica de interacción para App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ConfigureLogger();
        }

        private void ConfigureLogger()
        {
          
        }
    }
}
