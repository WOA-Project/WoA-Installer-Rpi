using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Installer.Core;
using Installer.Core.FullFx;
using MahApps.Metro.Controls.Dialogs;
using ReactiveUI;
using Serilog;

namespace Intaller.Wpf
{
    public class DualBootViewModel : ReactiveObject
    {
        private bool isCapable;
        private bool isEnabled;
        private bool isUpdated;

        public DualBootViewModel(IDialogCoordinator dialogCoordinator)
        {
            var isChangingDualBoot = new Subject<bool>();

            UpdateStatusWrapper =
                new CommandWrapper<Unit, DualBootStatus>(this, ReactiveCommand.CreateFromTask(GetStatus, isChangingDualBoot),
                    dialogCoordinator);

            UpdateStatusWrapper.Command.Subscribe(x =>
            {
                IsCapable = x.CanDualBoot;
                IsEnabled = x.IsEnabled;
                IsUpdated = true;
            });

            var canChangeDualBoot = UpdateStatusWrapper.Command.IsExecuting.Select(isExecuting => !isExecuting);

            EnableDualBootWrapper = new CommandWrapper<Unit, Unit>(this,
                ReactiveCommand.CreateFromTask(EnableDualBoot,
                    this.WhenAnyValue(x => x.IsCapable, x => x.IsEnabled,
                            (isCapable, isEnabled) => isCapable && !isEnabled)
                        .Merge(canChangeDualBoot)), dialogCoordinator);
            EnableDualBootWrapper.Command.Subscribe(async _ =>
            {
                await dialogCoordinator.ShowMessageAsync(this, "Done", "Dual Bool Enabled!");
                IsEnabled = !IsEnabled;
            });

            DisableDualBootWrapper = new CommandWrapper<Unit, Unit>(this,
                ReactiveCommand.CreateFromTask(DisableDualBoot,
                    this.WhenAnyValue(x => x.IsCapable, x => x.IsEnabled,
                            (isCapable, isEnabled) => isCapable && isEnabled)
                        .Merge(canChangeDualBoot)), dialogCoordinator);

            DisableDualBootWrapper.Command.Subscribe(async _ =>
            {
                await dialogCoordinator.ShowMessageAsync(this, "Done", "Dual Boot Disabled!");
                IsEnabled = !IsEnabled;
            });

            
            DisableDualBootWrapper.Command.IsExecuting.Select(x => !x).Subscribe(isChangingDualBoot);
            EnableDualBootWrapper.Command.IsExecuting.Select(x => !x).Subscribe(isChangingDualBoot);

            IsBusyObs = Observable.Merge(DisableDualBootWrapper.Command.IsExecuting,
                EnableDualBootWrapper.Command.IsExecuting, UpdateStatusWrapper.Command.IsExecuting);
        }

        public CommandWrapper<Unit, Unit> DisableDualBootWrapper { get; set; }

        public CommandWrapper<Unit, Unit> EnableDualBootWrapper { get; set; }

        public CommandWrapper<Unit, DualBootStatus> UpdateStatusWrapper { get; }

        public bool IsCapable
        {
            get => isCapable;
            set => this.RaiseAndSetIfChanged(ref isCapable, value);
        }

        public bool IsEnabled
        {
            get => isEnabled;
            set => this.RaiseAndSetIfChanged(ref isEnabled, value);
        }

        public bool IsUpdated
        {
            get => isUpdated;
            set => this.RaiseAndSetIfChanged(ref isUpdated, value);
        }

        public IObservable<bool> IsBusyObs { get; set; }

        private async Task EnableDualBoot()
        {
            
            var phone = await Phone.Load(new LowLevelApi());
            await phone.EnableDualBoot(true);
        }

        private async Task DisableDualBoot()
        {
            var phone = await Phone.Load(new LowLevelApi());
            await phone.EnableDualBoot(false);
        }

        private async Task<DualBootStatus> GetStatus()
        {
            var phone = await Phone.Load(new LowLevelApi());
            var status = await phone.GetDualBootStatus();
         
            return status;
        }
    }
}