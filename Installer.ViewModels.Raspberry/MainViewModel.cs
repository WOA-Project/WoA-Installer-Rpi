using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using Installer.Core;
using Installer.Core.Exceptions;
using Installer.Core.FileSystem;
using Installer.Core.Services;
using Installer.Core.Services.Wim;
using Installer.Raspberry.Core;
using Installer.UI;
using Installer.ViewModels.Core;
using ReactiveUI;
using Serilog;
using Serilog.Events;

namespace Installer.Raspberry.ViewModels
{
    public class MainViewModel : ReactiveObject, IDisposable
    {
        private readonly IDeployer<RaspberryPi> deployer;
        private readonly IPackageImporterFactory importerFactory;
        private readonly ObservableAsPropertyHelper<IEnumerable<DiskViewModel>> disksHelper;
        private readonly ObservableAsPropertyHelper<bool> hasWimHelper;
        private readonly ObservableAsPropertyHelper<bool> isBusyHelper;

        private readonly ObservableAsPropertyHelper<bool> isProgressVisibleHelper;
        private ReadOnlyObservableCollection<RenderedLogEvent> logEvents;

        private readonly ObservableAsPropertyHelper<double> progressHelper;
        private readonly ISubject<double> progressSubject = new BehaviorSubject<double>(double.NaN);
        private readonly ISettingsService settingsService;
        private ObservableAsPropertyHelper<RenderedLogEvent> statusHelper;
        private readonly UIServices uiServices;
        private ObservableAsPropertyHelper<WimMetadataViewModel> pickWimFileObs;
        private DiskViewModel selectedDisk;
        private IDisposable logLoader;

        public MainViewModel(IObservable<LogEvent> events, IDeployer<RaspberryPi> deployer, IPackageImporterFactory importerFactory, DiskService diskService,
            UIServices uiServices, ISettingsService settingsService)
        {
           

            this.deployer = deployer;
            this.importerFactory = importerFactory;
            this.uiServices = uiServices;
            this.settingsService = settingsService;
            RefreshDisksCommmandWrapper = new CommandWrapper<Unit, ICollection<Disk>>(this,
                ReactiveCommand.CreateFromTask(diskService.GetDisks), uiServices.DialogService);
            disksHelper = RefreshDisksCommmandWrapper.Command
                .Select(x => x
                    .Where(y => !y.IsBoot && !y.IsSystem && !y.IsOffline)
                    .Select(disk => new DiskViewModel(disk)))
                .ToProperty(this, x => x.Disks);

            ShowWarningCommand = ReactiveCommand.CreateFromTask(() => uiServices.DialogService.ShowAlert(this, Resources.TermsOfUseTitle,
                Resources.WarningNotice));

            ImportDriverPackageWrapper = new CommandWrapper<Unit, Unit>(this,
                ReactiveCommand.CreateFromTask(ImportDriverPackage), uiServices.DialogService);

            SetupPickWimCommand();

            var whenAnyValue = this.WhenAnyValue(x => x.SelectedDisk, (DiskViewModel disk) => disk != null);

            var canDeploy = this.WhenAnyObservable(x => x.WimMetadata.SelectedImageObs)
                .Select(metadata => metadata != null)
                .CombineLatest(whenAnyValue, (isWimSelected, isDiskSelected) => isDiskSelected && isWimSelected);

            FullInstallWrapper = new CommandWrapper<Unit, Unit>(this,
                ReactiveCommand.CreateFromTask(DeployUefiAndWindows, canDeploy), uiServices.DialogService);

            var isBusyObs = FullInstallWrapper.Command.IsExecuting;

            isBusyHelper = isBusyObs.ToProperty(this, model => model.IsBusy);

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
        }

        public CommandWrapper<Unit, Unit> ImportDriverPackageWrapper { get; set; }

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

        public WimMetadataViewModel WimMetadata => pickWimFileObs.Value;

        public bool HasWim => hasWimHelper.Value;

        public RenderedLogEvent Status => statusHelper.Value;

        public CommandWrapper<Unit, Unit> FullInstallWrapper { get; set; }

        private IObservable<WimMetadataViewModel> PickWimFileObs
        {
            get
            {
                var value = uiServices.FilePicker.Pick(
                    new List<(string, IEnumerable<string>)> {("WIM files", new[] {"install.wim"})},
                    () => settingsService.WimFolder, x => settingsService.WimFolder = x);

                return Observable.Return(value).Where(x => x != null)
                    .Select(LoadWimMetadata);
            }
        }

        public ReactiveCommand<Unit, WimMetadataViewModel> PickWimFileCommand { get; set; }

        public CommandWrapper<Unit, ICollection<Disk>> RefreshDisksCommmandWrapper { get; set; }

        public object ShowWarningCommand { get; }

        public bool IsBusy => isBusyHelper.Value;

        public ReadOnlyObservableCollection<RenderedLogEvent> Events => logEvents;

        public IEnumerable<DiskViewModel> Disks => disksHelper.Value;

        public DiskViewModel SelectedDisk
        {
            get => selectedDisk;
            set => this.RaiseAndSetIfChanged(ref selectedDisk, value);
        }

        public double Progress => progressHelper.Value;

        public bool IsProgressVisible => isProgressVisibleHelper.Value;

        private async Task DeployUefiAndWindows()
        {
            var installOptions = new InstallOptions(WimMetadata.Path)
            {
                ImageIndex = WimMetadata.SelectedDiskImage.Index
            };

            var raspberryPi = new RaspberryPi(SelectedDisk.Disk);
            await deployer.DeployCoreAndWindows(installOptions, raspberryPi, progressSubject);
            await uiServices.DialogService.ShowAlert(this, Resources.Finished,
                Resources.WindowsDeployedSuccessfully);
        }

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

        public void Dispose()
        {
            disksHelper?.Dispose();
            hasWimHelper?.Dispose();
            isBusyHelper?.Dispose();
            isProgressVisibleHelper?.Dispose();
            progressHelper?.Dispose();
            statusHelper?.Dispose();
            pickWimFileObs?.Dispose();
            logLoader?.Dispose();
            PickWimFileCommand?.Dispose();
        }
    }
}