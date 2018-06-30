using System.IO;

namespace Installer.Core
{
    public class Config
    {
        public DriveInfo EfiespDrive { get; }

        public Config(DriveInfo efiespDrive, Disk phoneDisk, Volume volume)
        {
            PhoneDisk = phoneDisk;
            EfiespDrive = efiespDrive;
            DataVolume = volume;
            BcdFileName = Path.Combine(efiespDrive.RootDirectory.Name, "EFI", "Microsoft", "Boot", "BCD");            
        }

        public string BcdFileName { get; }
        public Disk PhoneDisk { get; }
        public Volume DataVolume { get; }
    }
}