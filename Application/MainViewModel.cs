using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows;
using DynamicData;
using Installer.Core;
using Installer.Core.FullFx;
using ReactiveUI;
using Serilog.Events;

namespace Intaller.Wpf
{
    public class MainViewModel : ReactiveObject
    {
        private readonly ObservableAsPropertyHelper<bool> isBusyHelper;
        private readonly ReadOnlyObservableCollection<RenderedLogEvent> logEvents;
        private readonly ObservableAsPropertyHelper<double> progressHelper;
        private readonly ISubject<double> progresSubject = new Subject<double>();
        private readonly ObservableAsPropertyHelper<RenderedLogEvent> statusHelper;
        private IDisposable logLoader;
        private string wimPath;

        public MainViewModel(IObservable<LogEvent> events)
        {
            FullInstallCommand = ReactiveCommand.CreateFromTask(() => new Setup(new LowLevelApi(), new DismImageService()).FullInstall(new InstallOptions {ImagePath = WimPath}, progresSubject));
            FullInstallCommand.ThrownExceptions.Subscribe(e => { MessageBox.Show($"Error: {e.Message}"); });
            isBusyHelper = FullInstallCommand.IsExecuting.ToProperty(this, model => model.IsBusy);
            WimPath = @"J:\sources\install.wim";
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
                .ToObservableChangeSet()
                .Transform(x => new RenderedLogEvent
                {
                    Message = x.RenderMessage(),
                    Level = x.Level
                })
                .Bind(out logEvents)
                .DisposeMany()
                .Subscribe();
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