using System;
using System.Collections.Generic;
using System.Linq;
using Installer.Core.Services.Wim;
using ReactiveUI;

namespace Intaller.Wpf
{
    public class WimViewModel : ReactiveObject
    {
        private DiskImageMetadata selectedDiskImage;

        public WimViewModel(WindowsImageMetadata windowsImageMetadata, string path)
        {
            WindowsImageMetadata = windowsImageMetadata;
            Path = path;
            SelectedImageObs = this.WhenAnyValue(x => x.SelectedDiskImage);
            SelectedDiskImage = Images.First();
        }

        private WindowsImageMetadata WindowsImageMetadata { get; }
        public string Path { get; }

        public IObservable<DiskImageMetadata> SelectedImageObs { get; }

        public ICollection<DiskImageMetadata> Images => WindowsImageMetadata.Images;

        public DiskImageMetadata SelectedDiskImage
        {
            get => selectedDiskImage;
            set => this.RaiseAndSetIfChanged(ref selectedDiskImage, value);
        }
    }
}