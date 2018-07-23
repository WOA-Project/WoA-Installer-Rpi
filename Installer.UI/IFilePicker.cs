using System.Collections.Generic;

namespace Installer.UI
{
    public interface IFilePicker
    {
        string InitialDirectory { get; set; }
        List<FileTypeFilter> FileTypeFilter { get; }
        string PickFile();
    }
}