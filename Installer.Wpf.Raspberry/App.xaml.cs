using System;
using System.IO;
using System.Windows;
using ManagedWimLib;

namespace Installer.Raspberry.Application
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
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
