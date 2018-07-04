using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows;
using CinchExtended.Services.Interfaces;
using DynamicData;
using Installer.Core;
using Installer.Core.FullFx;
using Intaller.Wpf.UIServices;
using ReactiveUI;
using Serilog.Events;

namespace Intaller.Wpf
{
    public class MainViewModel : ReactiveObject
    {
        private readonly IMessageBoxService messageBoxService;
        private readonly ObservableAsPropertyHelper<bool> isBusyHelper;
        private readonly ReadOnlyObservableCollection<RenderedLogEvent> logEvents;
        private readonly ObservableAsPropertyHelper<double> progressHelper;
        private readonly ISubject<double> progresSubject = new Subject<double>();
        private readonly ObservableAsPropertyHelper<RenderedLogEvent> statusHelper;
        private IDisposable logLoader;
        private string wimPath;
        private int wimIndex;

        public MainViewModel(IObservable<LogEvent> events, IOpenFileService openFileService, IMessageBoxService messageBoxService)
        {
            this.messageBoxService = messageBoxService;
            var canFullInstall = this.WhenAnyValue(x => x.WimPath, x => x.WimIndex, (p, i) => !string.IsNullOrEmpty(p) && i >= 1);

            SetupPickWimCommand(openFileService);

            FullInstallCommand = ReactiveCommand.CreateFromTask(RunFullInstall, canFullInstall);
            FullInstallCommand.ThrownExceptions.Subscribe(e => { MessageBox.Show($"Error: {e.Message}"); });

            WindowsInstallCommand = ReactiveCommand.CreateFromTask(RunWindowsInstall, canFullInstall);
            WindowsInstallCommand.ThrownExceptions.Subscribe(e => { messageBoxService.ShowError($"Error: {e.Message}"); });

            isBusyHelper = FullInstallCommand.IsExecuting.ToProperty(this, model => model.IsBusy);
            progressHelper = progresSubject
                .ObserveOnDispatcher()
                .ToProperty(this, model => model.Progress);

            statusHelper = events
                .Where(x => x.Level == LogEventLevel.Information)
                .Select(x => new RenderedLogEvent
                {
                    Message = x.RenderMessage(),
                    Level = x.Level
                })
                .ToProperty(this, x => x.Status);

            logLoader = events
                .Where(x => x.Level == LogEventLevel.Information)
                .ToObservableChangeSet()
                .Transform(x => new RenderedLogEvent
                {
                    Message = x.RenderMessage(),
                    Level = x.Level
                })
                .Bind(out logEvents)
                .DisposeMany()
                .Subscribe();

            WimIndex = 1;
            WimPath = @"J:\sources\install.wim";
        }

        public ReactiveCommand<Unit, Unit> WindowsInstallCommand { get; set; }

        private void SetupPickWimCommand(IOpenFileService openFileService)
        {
            PickWimCommand = ReactiveCommand.Create(() =>
            {
                openFileService.Filter = "WIM files|*.wim";

                var showDialog = openFileService.ShowDialog(null);
                if (showDialog == true)
                {
                    return openFileService.FileName;
                }

                return null;
            });

            PickWimCommand
                .Where(string.IsNullOrEmpty)
                .Subscribe(path => { WimPath = path; });
        }

        public ReactiveCommand<Unit, string> PickWimCommand { get; set; }

        private async Task RunFullInstall()
        {
            var installOptions = new InstallOptions
            {
                ImagePath = WimPath,
                ImageIndex = WimIndex,
            };

            var setup = new Setup(new LowLevelApi(), new DismImageService());
            await setup.FullInstall(installOptions, progresSubject);
            messageBoxService.ShowInformation(@"Done!\nYou can now reboot your phone and choose ""Windows 10"" to start the Windows 10 ARM Setup.\nEnjoy!");
        }

        private async Task RunWindowsInstall()
        {
            var installOptions = new InstallOptions
            {
                ImagePath = WimPath,
                ImageIndex = WimIndex,
            };

            var setup = new Setup(new LowLevelApi(), new DismImageService());
            await setup.WindowsInstall(installOptions, progresSubject);
            messageBoxService.ShowInformation(@"Done!\nYou can now reboot your phone and choose ""Windows 10"" to start the Windows 10 ARM Setup.\nEnjoy!");
        }

        public int WimIndex
        {
            get => wimIndex;
            set => this.RaiseAndSetIfChanged(ref wimIndex, value);
        }

        public ReadOnlyObservableCollection<RenderedLogEvent> Events => logEvents;

        public RenderedLogEvent Status => statusHelper.Value;

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