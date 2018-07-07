using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using DynamicData;
using Installer.Core;
using Installer.Core.FullFx;
using Intaller.Wpf.Properties;
using Intaller.Wpf.UIServices;
using MahApps.Metro.Controls.Dialogs;
using ReactiveUI;
using Serilog;
using Serilog.Events;

namespace Intaller.Wpf
{
    public class MainViewModel : ReactiveObject, IDisposable
    {
        private readonly IDialogCoordinator dlgCoord;
        private readonly ObservableAsPropertyHelper<bool> isBusyHelper;
        private readonly ReadOnlyObservableCollection<RenderedLogEvent> logEvents;
        private readonly ObservableAsPropertyHelper<double> progressHelper;
        private readonly ISubject<double> progressSubject = new BehaviorSubject<double>(double.NaN);
        private readonly ObservableAsPropertyHelper<RenderedLogEvent> statusHelper;
        private readonly IDisposable logLoader;
        private string wimPath;
        private int wimIndex;
        private readonly Setup setup;
        private readonly ObservableAsPropertyHelper<bool> isProgressVisibleHelper;

        public MainViewModel(IObservable<LogEvent> events, IOpenFileService openFileService, IDialogCoordinator dlgCoord)
        {
            DualBootViewModel = new DualBootViewModel(dlgCoord);

            setup = new Setup(new LowLevelApi(), new DismImageService());

            this.dlgCoord = dlgCoord;
            var canDeploy = this.WhenAnyValue(x => x.WimPath, x => x.WimIndex, (p, i) => !string.IsNullOrEmpty(p) && i >= 1);

            ShowWarningCommand = ReactiveCommand.CreateFromTask(() => dlgCoord.ShowMessageAsync(this, "Warning", Resources.WarningNotice));

            SetupPickWimCommand(openFileService);

            FullInstallWrapper = new CommandWrapper<Unit, Unit>(this, ReactiveCommand.CreateFromTask(DeployEufiAndWindows, canDeploy), dlgCoord);
            WindowsInstallWrapper = new CommandWrapper<Unit, Unit>(this, ReactiveCommand.CreateFromTask(DeployWindows, canDeploy), dlgCoord);

            var isBusyObs = Observable.Merge(FullInstallWrapper.Command.IsExecuting, WindowsInstallWrapper.Command.IsExecuting);
            var dualBootIsBusyObs = DualBootViewModel.IsBusyObs;

            isBusyHelper = Observable.Merge(isBusyObs, dualBootIsBusyObs)
                .ToProperty(this, model => model.IsBusy);

            progressHelper = progressSubject
                .Where(d => !double.IsNaN(d))
                .ObserveOnDispatcher()
                .ToProperty(this, model => model.Progress);

            isProgressVisibleHelper = progressSubject
                    .Select(d => !double.IsNaN(d))
                    .ToProperty(this, x => x.IsProgressVisible);

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
            WimPath = "";
        }

        public ReactiveCommand<Unit, MessageDialogResult> ShowWarningCommand { get; set; }

        public bool IsProgressVisible => isProgressVisibleHelper.Value;

        private async Task HandleException(Exception e)
        {
            Log.Error(e, "An error has ocurred");
            await dlgCoord.ShowMessageAsync(this, "Error", $"{e.Message}");   
        }

        public CommandWrapper<Unit, Unit> WindowsInstallWrapper { get; set; }

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
                .Where(s => !string.IsNullOrEmpty(s))
                .Subscribe(path => { WimPath = path; });
        }

        public ReactiveCommand<Unit, string> PickWimCommand { get; set; }

        private async Task DeployEufiAndWindows()
        {
            var installOptions = new InstallOptions
            {
                ImagePath = WimPath,
                ImageIndex = WimIndex,
            };

            await setup.DeployUefiAndWindows(installOptions, progressSubject);
            await dlgCoord.ShowMessageAsync(this, "Finished", Resources.WindowsDeployedSuccessfully);
        }

        private async Task DeployWindows()
        {
            var installOptions = new InstallOptions
            {
                ImagePath = WimPath,
                ImageIndex = WimIndex,
            };

            await setup.DeployWindows(installOptions, progressSubject);
            await dlgCoord.ShowMessageAsync(this, "Finished", Resources.WindowsDeployedSuccessfully);
        }

        public int WimIndex
        {
            get => wimIndex;
            set => this.RaiseAndSetIfChanged(ref wimIndex, value);
        }

        public ReadOnlyObservableCollection<RenderedLogEvent> Events => logEvents;

        public RenderedLogEvent Status => statusHelper.Value;

        public bool IsBusy => isBusyHelper.Value;

        public CommandWrapper<Unit, Unit> FullInstallWrapper { get; set; }
        public double Progress => progressHelper.Value;

        public string WimPath
        {
            get => wimPath;
            set => this.RaiseAndSetIfChanged(ref wimPath, value);
        }

        public DualBootViewModel DualBootViewModel { get; }

        public void Dispose()
        {
            isBusyHelper?.Dispose();
            progressHelper?.Dispose();
            statusHelper?.Dispose();
            logLoader?.Dispose();
            isProgressVisibleHelper?.Dispose();
            PickWimCommand?.Dispose();
        }
    }
}