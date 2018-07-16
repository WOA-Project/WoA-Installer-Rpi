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
using Installer.Core.Services.Wim;
using Intaller.Wpf.Properties;
using MahApps.Metro.Controls.Dialogs;
using ReactiveUI;
using Serilog;
using Serilog.Events;

namespace Intaller.Wpf.ViewModels
{
    public class MainViewModel : ReactiveObject, IDisposable
    {
        private readonly Func<Task<Phone>> getPhoneFunc;
        private readonly ObservableAsPropertyHelper<bool> isBusyHelper;
        private readonly ReadOnlyObservableCollection<RenderedLogEvent> logEvents;
        private readonly ObservableAsPropertyHelper<double> progressHelper;
        private readonly ISubject<double> progressSubject = new BehaviorSubject<double>(double.NaN);
        private readonly ObservableAsPropertyHelper<RenderedLogEvent> statusHelper;
        private readonly IDisposable logLoader;
        private readonly ICollection<DriverPackageImporterItem> driverPackageImporterItems;
        private readonly ViewServices viewServices;
        private readonly ObservableAsPropertyHelper<bool> isProgressVisibleHelper;
        private readonly ObservableAsPropertyHelper<bool> hasWimHelper;
        private ObservableAsPropertyHelper<WimMetadataViewModel> pickWimFileObs;
        private DeployerItem selectedDeployerItem;

        public MainViewModel(IObservable<LogEvent> logEvents, ICollection<DeployerItem> deployersItems, ICollection<DriverPackageImporterItem> driverPackageImporterItems, ViewServices viewServices, Func<Task<Phone>> getPhoneFunc)
        {
            DualBootViewModel = new DualBootViewModel(viewServices.DialogCoordinator);

            DeployersItems = deployersItems;

            this.driverPackageImporterItems = driverPackageImporterItems;
            this.viewServices = viewServices;
            this.getPhoneFunc = getPhoneFunc;

            ShowWarningCommand = ReactiveCommand.CreateFromTask(() => viewServices.DialogCoordinator.ShowMessageAsync(this, Resources.TermsOfUseTitle, Resources.WarningNotice));

            SetupPickWimCommand();

            var isDeployerSelected = this.WhenAnyValue(model => model.SelectedDeployerItem, (DeployerItem x) => x != null);
            var isSelectedWim = this.WhenAnyObservable(x => x.WimMetadata.SelectedImageObs)
                .Select(metadata => metadata != null);

            var canDeploy = isSelectedWim.CombineLatest(isDeployerSelected, (hasWim, hasDeployer) => hasWim && hasDeployer);

            FullInstallWrapper = new CommandWrapper<Unit, Unit>(this, ReactiveCommand.CreateFromTask(DeployUefiAndWindows, canDeploy), viewServices.DialogCoordinator);
            WindowsInstallWrapper = new CommandWrapper<Unit, Unit>(this, ReactiveCommand.CreateFromTask(DeployWindows, canDeploy), viewServices.DialogCoordinator);
            InjectDriversWrapper = new CommandWrapper<Unit, Unit>(this, ReactiveCommand.CreateFromTask(InjectPostOobeDrivers, isDeployerSelected), viewServices.DialogCoordinator);

            ImportDriverPackageWrapper = new CommandWrapper<Unit, Unit>(this, ReactiveCommand.CreateFromTask(ImportDriverPackage), viewServices.DialogCoordinator);

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

        private async Task ImportDriverPackage()
        {
            var extensions = driverPackageImporterItems.Select(x => $"*.{x.Extension}");
            var fileName = PickFileName(viewServices.OpenFileService, "Driver package", extensions,
                () => Settings.Default.DriverPackFolder, fn => Settings.Default.DriverPackFolder = fn);

            var item = GetImporterItemForFile(fileName);
            var importer = item.DriverPackageImporter;
            
            var message = await importer.GetReadmeText(fileName);
            if (!string.IsNullOrEmpty(message))
            {
                viewServices.VisualizerService.Show("TextViewer", new MessageViewModel("Changelog", message), (_, __) => { }, OwnerOption.MainWindow);
            }

            await importer.ImportDriverPackage(fileName, "", progressSubject);
            await viewServices.DialogCoordinator.ShowMessageAsync(this, "Done", "Driver Package imported");
            Log.Information("Driver Package imported");
        }

        private DriverPackageImporterItem GetImporterItemForFile(string fileName)
        {
            var extension = Path.GetExtension(fileName);

            var importerItem = driverPackageImporterItems.First(item => string.Equals(extension, "." + item.Extension , StringComparison.InvariantCultureIgnoreCase));
            return importerItem;
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
                viewServices.DialogCoordinator.ShowMessageAsync(this, "Invalid WIM file", e.Message);
            });            
        }

        private string PickWimFile()
        {
            var fileName = PickFileName(viewServices.OpenFileService, "WIM Files", new[] { "*.wim" }, () => Settings.Default.WimFolder, x => Settings.Default.WimFolder = x);
            Log.Verbose("A WIM file has been selected: {FileName}", fileName);
            
            var defaultWimFolder = Path.GetDirectoryName(fileName);
            Settings.Default.WimFolder = defaultWimFolder;
            
            return fileName;
        }

        private static string PickFileName(IOpenFileService openFileService, string description, IEnumerable<string> extensions,
            Func<string> getCurrentFolder,
            Action<string> setCurrentFolder)
        {
            var extStr = string.Join(";" , extensions);

            openFileService.Filter = $"{description}|{extStr}";
            openFileService.FileName = "";
            openFileService.InitialDirectory = getCurrentFolder();
            if (openFileService.ShowDialog(null) == true)
            {
                var pickFileName = openFileService.FileName;
                var directoryName = Path.GetDirectoryName(pickFileName);
                setCurrentFolder(directoryName);
                Log.Verbose("Default directory for WimFolder has been set to {Folder}", directoryName);
                return pickFileName;
            }

            return null;
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
            await viewServices.DialogCoordinator.ShowMessageAsync(this, Resources.Finished, Resources.WindowsDeployedSuccessfully);
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
            await viewServices.DialogCoordinator.ShowMessageAsync(this, Resources.Finished, Resources.WindowsDeployedSuccessfully);
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

            await viewServices.DialogCoordinator.ShowMessageAsync(this, Resources.Finished, Resources.DriversInjectedSucessfully);
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