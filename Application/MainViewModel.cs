using System;
using System.Reactive;
using System.Windows;
using Installer.Core;
using Installer.Core.FullFx;
using ReactiveUI;

namespace Intaller.Wpf
{
    public class MainViewModel : ReactiveObject
    {
        private readonly ObservableAsPropertyHelper<bool> isBusyHelper;

        public MainViewModel()
        {
            FullInstallCommand = ReactiveCommand.CreateFromTask(() => new Setup(new LowLevelApi()).FullInstall(new InstallOptions()));
            FullInstallCommand.ThrownExceptions.Subscribe(e => { MessageBox.Show($"Error: {e}"); });
            isBusyHelper = FullInstallCommand.IsExecuting.ToProperty(this, model => model.IsBusy);
        }

        public bool IsBusy => isBusyHelper.Value;

        public ReactiveCommand<Unit, Unit> FullInstallCommand { get; set; }
    }
}