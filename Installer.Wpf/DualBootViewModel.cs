using System;
using System.Reactive;
using System.Threading.Tasks;
using Installer.Core;
using Installer.Core.FullFx;
using MahApps.Metro.Controls.Dialogs;
using ReactiveUI;

namespace Intaller.Wpf
{
    public class DualBootViewModel : ReactiveObject
    {
        private bool isCapable;
        private bool isEnabled;
        private bool isUpdated;

        public DualBootViewModel(IDialogCoordinator dialogCoordinator)
        {
            UpdateStatusWrapper = new CommandWrapper<Unit, DualBootStatus>(this, ReactiveCommand.CreateFromTask(GetStatus), dialogCoordinator);

            UpdateStatusWrapper.Command.Subscribe(x =>
            {
                IsCapable = x.CanDualBoot;
                IsEnabled = x.IsEnabled;
                IsUpdated = true;
            });

            EnableDualBootWrapper = new CommandWrapper<Unit, Unit>(this, ReactiveCommand.CreateFromTask(EnableDualBoot, this.WhenAnyValue(x => x.IsCapable, x => x.IsEnabled, (c, e) => c && !e)), dialogCoordinator);
            DisableDualBootWrapper = new CommandWrapper<Unit, Unit>(this, ReactiveCommand.CreateFromTask(DisableDualBoot, this.WhenAnyValue(x => x.IsCapable, x => x.IsEnabled, (c, e) => c && e)), dialogCoordinator);
        }

        public CommandWrapper<Unit, Unit> DisableDualBootWrapper { get; set; }

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

        public CommandWrapper<Unit, Unit> EnableDualBootWrapper { get; set; }

        public CommandWrapper<Unit, DualBootStatus> UpdateStatusWrapper { get; }

        private async Task<DualBootStatus> GetStatus()
        {
            var phone = await Phone.Load(new LowLevelApi());
            return await phone.GetDualBootStatus();
        }

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
    }
}