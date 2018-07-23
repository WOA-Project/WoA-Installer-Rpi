using System;

namespace Installer.UI
{
    public interface IViewService
    {
        void Register(string token, Type viewType);
        void Show(string key, object viewModel);
    }
}