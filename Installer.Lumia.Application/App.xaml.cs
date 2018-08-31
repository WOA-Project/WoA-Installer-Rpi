using System;
using System.IO;
using System.Windows;
using ManagedWimLib;

namespace Installer.Lumia.Application
{
    /// <summary>
    /// Lógica de interacción para App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            InitWimLib();
        }

        private static void InitWimLib()
        {
            if (Environment.Is64BitProcess)
            {
                Wim.GlobalInit(Path.Combine("x64", "libwim-15.dll"));
            }
            else
            {
                Wim.GlobalInit(Path.Combine("x86", "libwim-15.dll"));
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Wim.GlobalCleanup();
            base.OnExit(e);
        }
    }
}
