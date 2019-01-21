using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ByteSizeLib;
using DynamicData;
using Installer.Core;
using Installer.Core.Exceptions;
using Installer.Core.FileSystem;
using Installer.Core.FullFx;
using Installer.Core.Services.Wim;
using Installer.Core.Utils;
using Installer.Lumia.Core;
using Installer.UI;
using Installer.ViewModels.Core;
using ReactiveUI;
using Serilog;
using Serilog.Events;

namespace Installer.Lumia.ViewModels
{
    public class MainViewModel : ReactiveObject, IDisposable
    {
        private const int VersionOfWindowsThatNeedsBootPatching = 17137;
        private readonly Func<Task<Phone>> getPhoneFunc;
        private readonly ObservableAsPropertyHelper<bool> isBusyHelper;
        private ReadOnlyObservableCollection<RenderedLogEvent> logEvents;
        private readonly ObservableAsPropertyHelper<double> progressHelper;
        private readonly ISubject<double> progressSubject = new BehaviorSubject<double>(double.NaN);
        private ObservableAsPropertyHelper<RenderedLogEvent> statusHelper;
        private IDisposable logLoader;
        private readonly IPackageImporterFactory importerFactory;
        private readonly UIServices uiServices;
        private readonly ISettingsService settingsService;
        private readonly ObservableAsPropertyHelper<bool> isProgressVisibleHelper;
        private readonly ObservableAsPropertyHelper<bool> hasWimHelper;
        private ObservableAsPropertyHelper<WimMetadataViewModel> pickWimFileObs;
        private DeployerItem selectedDeployerItem;
        private readonly ObservableAsPropertyHelper<ByteSize> sizeReservedForWindows;

        public MainViewModel(IObservable<LogEvent> events, ICollection<DeployerItem> deployersItems, IPackageImporterFactory importerFactory, UIServices uiServices, ISettingsService settingsService, Func<Task<Phone>> getPhoneFunc)
        {
            DualBootViewModel = new DualBootViewModel(uiServices.DialogService, getPhoneFunc);

            DeployersItems = deployersItems;

            this.importerFactory = importerFactory;
            this.uiServices = uiServices;
            this.settingsService = settingsService;
            this.getPhoneFunc = getPhoneFunc;

            ShowWarningCommand = ReactiveCommand.CreateFromTask(() =>
                uiServices.DialogService.ShowAlert(this, Resources.TermsOfUseTitle,
                    Resources.WarningNotice));

            SetupPickWimCommand();

            var isDeployerSelected =
                this.WhenAnyValue(model => model.SelectedDeployerItem, (DeployerItem x) => x != null);
            var isSelectedWim = this.WhenAnyObservable(x => x.WimMetadata.SelectedImageObs)
                .Select(metadata => metadata != null);

            var canDeploy =
                isSelectedWim.CombineLatest(isDeployerSelected, (hasWim, hasDeployer) => hasWim && hasDeployer);

            FullInstallWrapper = new CommandWrapper<Unit, Unit>(this,
                ReactiveCommand.CreateFromTask(DeployUefiAndWindows, canDeploy), uiServices.DialogService);
            WindowsInstallWrapper = new CommandWrapper<Unit, Unit>(this,
                ReactiveCommand.CreateFromTask(DeployWindows, canDeploy), uiServices.DialogService);
            InjectDriversWrapper = new CommandWrapper<Unit, Unit>(this,
                ReactiveCommand.CreateFromTask(InjectPostOobeDrivers, isDeployerSelected),
                uiServices.DialogService);

            InstallGpuWrapper = new CommandWrapper<Unit, Unit>(this,
                ReactiveCommand.CreateFromTask(InstallGpu), uiServices.DialogService);

            ImportDriverPackageWrapper = new CommandWrapper<Unit, Unit>(this,
                ReactiveCommand.CreateFromTask(ImportDriverPackage), uiServices.DialogService);

            var isBusyObs = Observable.Merge(FullInstallWrapper.Command.IsExecuting,
                WindowsInstallWrapper.Command.IsExecuting,
                InjectDriversWrapper.Command.IsExecuting,
                ImportDriverPackageWrapper.Command.IsExecuting,
                InstallGpuWrapper.Command.IsExecuting);

            var dualBootIsBusyObs = DualBootViewModel.IsBusyObs;

            isBusyHelper = Observable.Merge(isBusyObs, dualBootIsBusyObs)
                .ToProperty(this, model => model.IsBusy);

            progressHelper = progressSubject
                .Where(d => !double.IsNaN(d))
                .ObserveOn(SynchronizationContext.Current)
                .ToProperty(this, model => model.Progress);

            isProgressVisibleHelper = progressSubject
                .Select(d => !double.IsNaN(d))
                .ToProperty(this, x => x.IsProgressVisible);

            SetupLogging(events);
            
            hasWimHelper = this.WhenAnyValue(model => model.WimMetadata, (WimMetadataViewModel x) => x != null)
                .ToProperty(this, x => x.HasWim);

            sizeReservedForWindows =
                this.WhenAnyValue(x => x.GbsReservedForWindows, ByteSize.FromGigaBytes)
                    .ToProperty(this, x => x.SizeReservedForWindows);
        }

        public CommandWrapper<Unit, Unit> InstallGpuWrapper { get; set; }

        public ByteSize SizeReservedForWindows => sizeReservedForWindows.Value;

        private void SetupLogging(IObservable<LogEvent> events)
        {
            var conn = events
                .ObserveOn(SynchronizationContext.Current)
                .Where(x => x.Level == LogEventLevel.Information)
                .Publish();

            statusHelper = conn
                .Select(RenderedLogEvent)
                .ToProperty(this, x => x.Status);

            logLoader = conn
                .ToObservableChangeSet()
                .Transform(RenderedLogEvent)
                .Bind(out logEvents)
                .DisposeMany()
                .Subscribe();

            conn.Connect();
        }

        private static RenderedLogEvent RenderedLogEvent(LogEvent x)
        {
            return new RenderedLogEvent
            {
                Message = x.RenderMessage(),
                Level = x.Level
            };
        }

        public IEnumerable<DeployerItem> DeployersItems { get; }

        public DeployerItem SelectedDeployerItem
        {
            get => selectedDeployerItem;
            set => this.RaiseAndSetIfChanged(ref selectedDeployerItem, value);
        }

        private IDeployer<Phone> SelectedDeployer => SelectedDeployerItem.Deployer;

        public bool HasWim => hasWimHelper.Value;

        private async Task ImportDriverPackage()
        {
            var extensions = importerFactory.ImporterKeys.Select(x => $"*.{x}");

            var fileName = uiServices.FilePicker.Pick(new List<(string, IEnumerable<string>)> { ("Core Package", extensions) }, () => settingsService.DriverPackFolder, fn => settingsService.DriverPackFolder = fn);

            if (fileName == null)
            {
                return;
            }

            var fileType = Path.GetExtension(fileName).Substring(1);
            var importer = importerFactory.GetImporter(fileType);

            var message = await importer.GetReadmeText(fileName);
            if (!string.IsNullOrEmpty(message))
            {
                uiServices.ViewService.Show("TextViewer", new MessageViewModel("Changelog", message));
            }

            await importer.Extract(fileName, progressSubject);
            await uiServices.DialogService.ShowAlert(this, "Done", "Core Package imported");
            Log.Information("Core Package imported");
        }

        public CommandWrapper<Unit, Unit> ImportDriverPackageWrapper { get; }

        public CommandWrapper<Unit, Unit> InjectDriversWrapper { get; }

        public ReactiveCommand<Unit, Unit> ShowWarningCommand { get; set; }

        public bool IsProgressVisible => isProgressVisibleHelper.Value;

        public CommandWrapper<Unit, Unit> WindowsInstallWrapper { get; set; }

        private void SetupPickWimCommand()
        {
            PickWimFileCommand = ReactiveCommand.CreateFromObservable(() => PickWimFileObs);
            pickWimFileObs = PickWimFileCommand.ToProperty(this, x => x.WimMetadata);
            PickWimFileCommand.ThrownExceptions.Subscribe(e =>
            {
                Log.Error(e, "WIM file error");
                uiServices.DialogService.ShowAlert(this, "Invalid WIM file", e.Message);
            });
        }

        private IObservable<WimMetadataViewModel> PickWimFileObs
        {
            get
            {
                var value = uiServices.FilePicker.Pick(new List<(string, IEnumerable<string>)> {("WIM files", new[] {"install.wim"})},
                    () => settingsService.WimFolder, x => settingsService.WimFolder = x);

                return Observable.Return(value).Where(x => x != null)
                    .Select(LoadWimMetadata);
            }
        }

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
            var version = int.TryParse(WimMetadata.SelectedDiskImage.Build, out var v) ? v : 0;
            
            var installOptions = new InstallOptions
            {
                ImagePath = WimMetadata.Path,
                ImageIndex = WimMetadata.SelectedDiskImage.Index,
                PatchBoot = version > VersionOfWindowsThatNeedsBootPatching
            };

            await SelectedDeployer.DeployCoreAndWindows(installOptions, await GetPhone(), progressSubject);
            await uiServices.DialogService.ShowAlert(this, Resources.Finished,
                Resources.WindowsDeployedSuccessfully);
        }

        private Task<Phone> GetPhone()
        {
            return getPhoneFunc();
        }

        private async Task InstallGpu()
        {
            var phone = await GetPhone();

            var panelFolder = @"Files\Cityman\Drivers\GPU\OEMPanel";

            var pathsToCheck = new[] { panelFolder,}; 

            if (!pathsToCheck.EnsureExistingPaths())
            {
                await uiServices.DialogService.ShowAlert(this, "Error", "The required files for the GPU installation are missing");
                return;
            }

            if (!(await phone.GetDualBootStatus()).IsEnabled)
            {
                await uiServices.DialogService.ShowAlert(this, "Error", "You should enable Dual Boot for the GPU installation to work");
                return;
            }

            var winVolume = await phone.GetWindowsVolume();

            var destPath = Path.Combine(winVolume.RootDir.Name, "Users", "Public", "OEMPanel");
            var publicDir = new DirectoryInfo(destPath);
            FileUtils.CreateDirectory(destPath);
            await FileUtils.CopyDirectory(new DirectoryInfo(panelFolder), publicDir);

            var messageViewModel = new MessageViewModel("Manual steps", ManualStepsText());

            uiServices.ViewService.Show("MarkdownViewer", messageViewModel);
        }

        private static string ManualStepsText()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Installer.Lumia.ViewModels.Gpu.md";

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream ?? throw new InvalidOperationException($"Cannot get stream for {resourceName}")))
            {
                return reader.ReadToEnd();
            }
        }

        private async Task DeployWindows()
        {
            var version = int.TryParse(WimMetadata.SelectedDiskImage.Build, out var v) ? v : 0;

            var installOptions = new InstallOptions
            {
                ImagePath = WimMetadata.Path,
                ImageIndex = WimMetadata.SelectedDiskImage.Index,
                PatchBoot = version > VersionOfWindowsThatNeedsBootPatching
            };

            await SelectedDeployer.DeployWindows(installOptions, await GetPhone(), progressSubject);
            await uiServices.DialogService.ShowAlert(this, Resources.Finished,
                Resources.WindowsDeployedSuccessfully);
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

            await uiServices.DialogService.ShowAlert(this, Resources.Finished,
                Resources.DriversInjectedSucessfully);
        }

        public ReadOnlyObservableCollection<RenderedLogEvent> Events => logEvents;

        public RenderedLogEvent Status => statusHelper.Value;

        public bool IsBusy => isBusyHelper.Value;

        public CommandWrapper<Unit, Unit> FullInstallWrapper { get; set; }
        public double Progress => progressHelper.Value;

        public DualBootViewModel DualBootViewModel { get; }

        public double GbsReservedForWindows
        {
            get => settingsService.SizeReservedForWindows;
            set
            {
                settingsService.SizeReservedForWindows = value;
                this.RaisePropertyChanged(nameof(GbsReservedForWindows));
            }
        }

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
            settingsService.Save();
        }
    }    
}