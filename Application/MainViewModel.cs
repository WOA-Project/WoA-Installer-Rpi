using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows;
using Installer.Core;
using Installer.Core.FullFx;
using ReactiveUI;

namespace Intaller.Wpf
{
    public class MainViewModel : ReactiveObject
    {
        private readonly ObservableAsPropertyHelper<bool> isBusyHelper;
        private string wimPath;
        private readonly ISubject<double> progresSubject = new BehaviorSubject<double>(0D);
        private readonly ObservableAsPropertyHelper<double> progressHelper;

        public MainViewModel()
        {
            FullInstallCommand = ReactiveCommand.CreateFromTask(() => new Setup(new LowLevelApi(), new DismImageService()).FullInstall(new InstallOptions() { ImagePath = WimPath }, progresSubject));
            FullInstallCommand.ThrownExceptions.Subscribe(e => { MessageBox.Show($"Error: {e.Message}"); });
            isBusyHelper = FullInstallCommand.IsExecuting.ToProperty(this, model => model.IsBusy);
            WimPath = @"F:\sources\install.wim";
            progressHelper = progresSubject
                .ObserveOnDispatcher()
                .ToProperty(this, model => model.Progress);
        }

        public bool IsBusy => isBusyHelper.Value;

        public ReactiveCommand<Unit, Unit> FullInstallCommand { get; set; }
        public double Progress => progressHelper.Value;

        public string WimPath
        {
            get => wimPath;
            set => this.RaiseAndSetIfChanged(ref wimPath, value);
        }
    }
}