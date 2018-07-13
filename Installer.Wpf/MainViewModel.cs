using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Cinch.Reloaded.Services.Implementation;
using Cinch.Reloaded.Services.Interfaces;
using DynamicData;
using Installer.Core;
using Installer.Core.Exceptions;
using Installer.Core.Services;
using Installer.Core.Services.Wim;
using Intaller.Wpf.Properties;
using MahApps.Metro.Controls.Dialogs;
using ReactiveUI;
using Serilog;
using Serilog.Events;

namespace Intaller.Wpf
{
    public class MainViewModel : ReactiveObject, IDisposable
    {
        private readonly IDialogCoordinator dlgCoord;
        private readonly IExtendedUIVisualizerService visualizerService;
        private readonly Func<Task<Phone>> getPhoneFunc;
        private readonly ObservableAsPropertyHelper<bool> isBusyHelper;
        private readonly ReadOnlyObservableCollection<RenderedLogEvent> logEvents;
        private readonly ObservableAsPropertyHelper<double> progressHelper;
        private readonly ISubject<double> progressSubject = new BehaviorSubject<double>(double.NaN);
        private readonly ObservableAsPropertyHelper<RenderedLogEvent> statusHelper;
        private readonly IDisposable logLoader;
        private readonly IDriverPackageImporter packageImporter;
        private readonly IOpenFileService openFileService;
        private readonly ObservableAsPropertyHelper<bool> isProgressVisibleHelper;
        private readonly ObservableAsPropertyHelper<bool> hasWimHelper;
        private ObservableAsPropertyHelper<WimMetadataViewModel> pickWimFileObs;
        private DeployerItem selectedDeployerItem;

        public MainViewModel(IObservable<LogEvent> logEvents, ICollection<DeployerItem> deployersItems, IDriverPackageImporter packageImporter, IOpenFileService openFileService, IDialogCoordinator dlgCoord, IExtendedUIVisualizerService visualizerService, Func<Task<Phone>> getPhoneFunc)
        {
            DualBootViewModel = new DualBootViewModel(dlgCoord);

            DeployersItems = deployersItems;

            this.packageImporter = packageImporter;
            this.openFileService = openFileService;
            this.dlgCoord = dlgCoord;
            this.visualizerService = visualizerService;
            this.getPhoneFunc = getPhoneFunc;

            ShowWarningCommand = ReactiveCommand.CreateFromTask(() => dlgCoord.ShowMessageAsync(this, Resources.TermsOfUseTitle, Resources.WarningNotice));

            SetupPickWimCommand();

            var isDeployerSelected = this.WhenAnyValue(model => model.SelectedDeployerItem, (DeployerItem x) => x != null);
            var isSelectedWim = this.WhenAnyObservable(x => x.WimMetadata.SelectedImageObs)
                .Select(metadata => metadata != null);

            var canDeploy = isSelectedWim.CombineLatest(isDeployerSelected, (hasWim, hasDeployer) => hasWim && hasDeployer);

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

            hasWimHelper = this.WhenAnyValue(model => model.WimMetadata, (WimMetadataViewModel x) => x != null).ToProperty(this, x => x.HasWim);
        }

        public IEnumerable<DeployerItem> DeployersItems { get; }

        public DeployerItem SelectedDeployerItem
        {
            get => selectedDeployerItem;
            set => this.RaiseAndSetIfChanged(ref selectedDeployerItem, value);
        }

        private IDeployer SelectedDeployer => SelectedDeployerItem.Deployer;

        public bool HasWim => hasWimHelper.Value;

        private async Task InstallDriverPackage()
        {
            openFileService.Filter = "7-Zip files|*.7z";
            openFileService.FileName = "";
            openFileService.InitialDirectory = Settings.Default.DriverPackFolder;

            var showDialog = openFileService.ShowDialog(null);
            if (showDialog != true)
            {
                return;
            }

            var fileName = openFileService.FileName;
            Settings.Default.DriverPackFolder = Path.GetDirectoryName(fileName);

            var message = await packageImporter.GetReadmeText(fileName);
            if (!string.IsNullOrEmpty(message))
            {
                visualizerService.Show("TextViewer", new MessageViewModel("Changelog", message), (_, __) => { }, OwnerOption.MainWindow);
            }

            // TODO:
            await packageImporter.ImportDriverPackage(fileName, "", progressSubject);
        }

        public CommandWrapper<Unit, Unit> ImportDriverPackageWrapper { get; }

        public CommandWrapper<Unit, Unit> InjectDriversWrapper { get; }

        public ReactiveCommand<Unit, MessageDialogResult> ShowWarningCommand { get; set; }

        public bool IsProgressVisible => isProgressVisibleHelper.Value;

        public CommandWrapper<Unit, Unit> WindowsInstallWrapper { get; set; }

        private void SetupPickWimCommand()
        {
            PickWimFileCommand = ReactiveCommand.CreateFromObservable(() => PickWimFileObs);

            pickWimFileObs = PickWimFileCommand.ToProperty(this, x => x.WimMetadata);


            PickWimFileCommand.ThrownExceptions.Subscribe(e =>
            {
                Log.Error(e, "WIM file error");
                dlgCoord.ShowMessageAsync(this, "Invalid WIM file", e.Message);
            });            
        }

        private string PickWimFile()
        {
            openFileService.Filter = "WIM files|*.wim";
            openFileService.FileName = "";
            openFileService.InitialDirectory = Settings.Default.WimFolder;

            var showDialog = openFileService.ShowDialog(null);

            if (showDialog != true)
            {
                return null;
            }

            var fileName = openFileService.FileName;
            Log.Verbose("A WIM file has been selected: {FileName}", fileName);


            var defaultWimFolder = Path.GetDirectoryName(fileName);
            Settings.Default.WimFolder = defaultWimFolder;
            Log.Verbose("Default directory for WimFolder has been set to {Folder}", defaultWimFolder);

            return fileName;
        }

        private IObservable<WimMetadataViewModel> PickWimFileObs =>
            Observable.Return(PickWimFile()).Where(x => !string.IsNullOrEmpty(x)).Select(LoadWimMetadata);

        public WimMetadataViewModel WimMetadata => pickWimFileObs.Value;

        private static WimMetadataViewModel LoadWimMetadata(string path)
        {
            Log.Verbose("Trying to load WIM metadata file at '{ImagePath}'", path);

            using (var file = File.OpenRead(path))
            {
                var imageReader = new WindowsImageMetadataReader();
                var windowsImageInfo = imageReader.Load(file);
                if (windowsImageInfo.Images.All(x => x.Architecture != Architecture.Arm64))
                {
                    throw new InvalidWimFileException(Resources.WimFileNoValidArchitecture);
                }

                var vm = new WimMetadataViewModel(windowsImageInfo, path);

                Log.Verbose("WIM metadata file at '{ImagePath}' retrieved correctly", path);

                return vm;
            }
        }

        public ReactiveCommand<Unit, WimMetadataViewModel> PickWimFileCommand { get; set; }

        private async Task DeployUefiAndWindows()
        {
            var installOptions = new InstallOptions
            {
                ImagePath = WimMetadata.Path,
                ImageIndex = WimMetadata.SelectedDiskImage.Index,
            };
            
            await SelectedDeployer.DeployCoreAndWindows(installOptions, await GetPhone(), progressSubject);
            await dlgCoord.ShowMessageAsync(this, Resources.Finished, Resources.WindowsDeployedSuccessfully);
        }

        private Task<Phone> GetPhone()
        {
            return getPhoneFunc();
        }

        private async Task DeployWindows()
        {
            var installOptions = new InstallOptions
            {
                ImagePath = WimMetadata.Path,
                ImageIndex = WimMetadata.SelectedDiskImage.Index,
            };

            await SelectedDeployer.DeployWindows(installOptions, await GetPhone(), progressSubject);
            await dlgCoord.ShowMessageAsync(this, Resources.Finished, Resources.WindowsDeployedSuccessfully);
        }

        private async Task InjectPostOobeDrivers()
        {
            try
            {
                await SelectedDeployer.InjectPostOobeDrivers(await GetPhone());
            }
            catch (DirectoryNotFoundException e)
            {
                throw new InvalidOperationException(Resources.NoPostOobeDrivers, e);
            }
            catch (InvalidOperationException e)
            {
                throw new InvalidOperationException(Resources.CannotInjectPostOobe, e);
            }

            await dlgCoord.ShowMessageAsync(this, Resources.Finished, Resources.DriversInjectedSucessfully);
        }

        public ReadOnlyObservableCollection<RenderedLogEvent> Events => logEvents;

        public RenderedLogEvent Status => statusHelper.Value;

        public bool IsBusy => isBusyHelper.Value;

        public CommandWrapper<Unit, Unit> FullInstallWrapper { get; set; }
        public double Progress => progressHelper.Value;

        public DualBootViewModel DualBootViewModel { get; }

        public void Dispose()
        {
            isBusyHelper?.Dispose();
            progressHelper?.Dispose();
            statusHelper?.Dispose();
            logLoader?.Dispose();
            isProgressVisibleHelper?.Dispose();
            hasWimHelper?.Dispose();
            ShowWarningCommand?.Dispose();
            PickWimFileCommand?.Dispose();
        }
    }
}