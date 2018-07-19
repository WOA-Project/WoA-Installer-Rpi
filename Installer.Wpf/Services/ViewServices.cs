using Cinch.Reloaded.Services.Interfaces;
using MahApps.Metro.Controls.Dialogs;

namespace Intaller.Wpf.Services
{
    public class ViewServices
    {
        public ViewServices(IOpenFileService openFileService, IDialogCoordinator dialogCoordinator, IExtendedUIVisualizerService visualizerService)
        {
            OpenFileService = openFileService;
            DialogCoordinator = dialogCoordinator;
            VisualizerService = visualizerService;
        }

        public IOpenFileService OpenFileService { get; }
        public IDialogCoordinator DialogCoordinator { get; }
        public IExtendedUIVisualizerService VisualizerService { get; }
    }
}