using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows.Input;
using DynamicData;
using Installer.Core;
using Intaller.Wpf.Properties;
using Intaller.Wpf.UIServices;
using MahApps.Metro.Controls.Dialogs;
using ReactiveUI;
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
        private readonly ISetup setup;
        private readonly IOpenFileService openFileService;
        private readonly ObservableAsPropertyHelper<bool> isProgressVisibleHelper;
        private string driverPackageLocation;
        private string message;
        private readonly ObservableAsPropertyHelper<bool> hasMessageHelper;

        public MainViewModel(IObservable<LogEvent> logEvents, ISetup setup, IOpenFileService openFileService, IDialogCoordinator dlgCoord)
        {
            DualBootViewModel = new DualBootViewModel(dlgCoord);

            this.setup = setup;
            this.openFileService = openFileService;
            this.dlgCoord = dlgCoord;

            var canDeploy = this.WhenAnyValue(x => x.WimPath, x => x.WimIndex, (p, i) => !string.IsNullOrEmpty(p) && i >= 1);

            ShowWarningCommand = ReactiveCommand.CreateFromTask(() => dlgCoord.ShowMessageAsync(this, Resources.TermsOfUseTitle, Resources.WarningNotice));

            SetupPickWimCommand(openFileService);

            FullInstallWrapper = new CommandWrapper<Unit, Unit>(this, ReactiveCommand.CreateFromTask(DeployUefiAndWindows, canDeploy), dlgCoord);
            WindowsInstallWrapper = new CommandWrapper<Unit, Unit>(this, ReactiveCommand.CreateFromTask(DeployWindows, canDeploy), dlgCoord);
            InjectDriversWrapper = new CommandWrapper<Unit, Unit>(this, ReactiveCommand.CreateFromTask(InjectPostOobeDrivers), dlgCoord);

            ImportDriverPackageWrapper = new CommandWrapper<Unit, Unit>(this, ReactiveCommand.CreateFromTask(InstallDriverPackage), dlgCoord);
            
            var isBusyObs = Observable.Merge(FullInstallWrapper.Command.IsExecuting, 
                WindowsInstallWrapper.Command.IsExecuting, 
                InjectDriversWrapper.Command.IsExecuting, 
                ImportDriverPackageWrapper.Command.IsExecuting);

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

            statusHelper = logEvents
                .Where(x => x.Level == LogEventLevel.Information)
                .Select(x => new RenderedLogEvent
                {
                    Message = x.RenderMessage(),
                    Level = x.Level
                })
                .ToProperty(this, x => x.Status);

            logLoader = logEvents
                .Where(x => x.Level == LogEventLevel.Information)
                .ToObservableChangeSet()
                .Transform(x => new RenderedLogEvent
                {
                    Message = x.RenderMessage(),
                    Level = x.Level
                })
                .Bind(out this.logEvents)
                .DisposeMany()
                .Subscribe();

            hasMessageHelper = this.WhenAnyValue(model => model.Message, (string s) => s != null).ToProperty(this, x => x.HasMessage);

            ClearMessageCommand = ReactiveCommand.Create(() => Message = null);

            WimIndex = 1;
            WimPath = "";
        }

        public ReactiveCommand<Unit, string> ClearMessageCommand { get; set; }

        public bool HasMessage => hasMessageHelper.Value;

        private async Task InstallDriverPackage()
        {
            openFileService.Filter = "7-Zip files|*.7z";

            var showDialog = openFileService.ShowDialog(null);
            if (showDialog != true)
            {
                return;
            }

            Message = await setup.GetDriverPackageReadmeText(openFileService.FileName);
            await setup.InstallDriverPackage(openFileService.FileName, progressSubject);
        }

        public string Message
        {
            get => message;
            set => this.RaiseAndSetIfChanged(ref message, value);
        }

        public string DriverPackageLocation
        {
            get => driverPackageLocation;
            set => this.RaiseAndSetIfChanged(ref driverPackageLocation, value);
        }

        public ReactiveCommand<Unit, string> PickDriverPackageCommand { get; set; }

        public CommandWrapper<Unit, Unit> ImportDriverPackageWrapper { get; }

        public CommandWrapper<Unit, Unit> InjectDriversWrapper { get; }

        public ReactiveCommand<Unit, MessageDialogResult> ShowWarningCommand { get; set; }

        public bool IsProgressVisible => isProgressVisibleHelper.Value;

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

        private async Task DeployUefiAndWindows()
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

        private async Task InjectPostOobeDrivers()
        {
            await setup.InjectPostOobeDrivers();
            await dlgCoord.ShowMessageAsync(this, "Finished", Resources.DriversInjectedSucessfully);
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