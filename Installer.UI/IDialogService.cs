using System.Threading.Tasks;

namespace Installer.UI
{
    public interface IDialogService
    {
        Task ShowAlert(object owner, string title, string text);
    }
}