using System;
using System.Collections.Generic;
using System.Linq;
using Installer.Core.Wim;
using ReactiveUI;

namespace Intaller.Wpf
{
    public class WimViewModel : ReactiveObject
    {
        private WindowsImageMetadataReader.ImageMetadata selectedImage;

        public WimViewModel(WindowsImageMetadataReader.WindowsImageInfo windowsImageInfo, string path)
        {
            WindowsImageInfo = windowsImageInfo;
            Path = path;
            SelectedImageObs = this.WhenAnyValue(x => x.SelectedImage);
            SelectedImage = Images.First();
        }

        private WindowsImageMetadataReader.WindowsImageInfo WindowsImageInfo { get; }
        public string Path { get; }

        public IObservable<WindowsImageMetadataReader.ImageMetadata> SelectedImageObs { get; }

        public ICollection<WindowsImageMetadataReader.ImageMetadata> Images => WindowsImageInfo.Images;

        public WindowsImageMetadataReader.ImageMetadata SelectedImage
        {
            get => selectedImage;
            set => this.RaiseAndSetIfChanged(ref selectedImage, value);
        }
    }
}