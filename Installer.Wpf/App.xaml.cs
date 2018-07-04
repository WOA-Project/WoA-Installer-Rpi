using System.Windows;

namespace Intaller.Wpf
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
