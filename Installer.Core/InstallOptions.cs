using System;

namespace Installer.Core
{
    public class InstallOptions
    {
        public InstallOptions()
        {            
        }

        public InstallOptions(string imagePath)
        {
            ImagePath = imagePath;
        }

        public string ImagePath { get; set; }
        public int ImageIndex { get; set; } = 1;
    }
}