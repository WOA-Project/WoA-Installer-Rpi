using System;
using System.Threading.Tasks;
using MahApps.Metro.Controls.Dialogs;
using ReactiveUI;
using Serilog;

namespace Intaller.Wpf
{
    public class CommandWrapper<T1, T2> : ReactiveObject
    {
        private readonly object parent;
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly ObservableAsPropertyHelper<bool> isExecutingHelper;
        public ReactiveCommand<T1, T2> Command { get; }

        public CommandWrapper(object parent, ReactiveCommand<T1, T2> command, IDialogCoordinator dialogCoordinator)
        {
            this.parent = parent;
            this.dialogCoordinator = dialogCoordinator;
            Command = command;
            command.ThrownExceptions.Subscribe(async e => await HandleException(e));
            isExecutingHelper = command.IsExecuting.ToProperty(this, x => x.IsExecuting);
        }

        private async Task HandleException(Exception e)
        {
            Log.Error(e, "An error has ocurred");
            Log.Information($"Error: {e.Message}");
            await dialogCoordinator.ShowMessageAsync(parent, "Error", $"{e.Message}");   
        }

        public bool IsExecuting => isExecutingHelper.Value;
    }
}