using System;
using System.Reactive;
using System.Windows;
using ReactiveUI;

namespace Install
{
    public class MainViewModel : ReactiveObject
    {
        private readonly ObservableAsPropertyHelper<bool> isBusyHelper;

        public MainViewModel(ISetup setup)
        {
            FullInstallCommand = ReactiveCommand.CreateFromTask(() => setup.FullInstall(new InstallOptions()));
            FullInstallCommand.ThrownExceptions.Subscribe(e => { MessageBox.Show($"Error: {e}"); });
            isBusyHelper = FullInstallCommand.IsExecuting.ToProperty(this, model => model.IsBusy);
        }

        public bool IsBusy => isBusyHelper.Value;

        public ReactiveCommand<Unit, Unit> FullInstallCommand { get; set; }
    }
}