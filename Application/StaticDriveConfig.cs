using System;
using System.IO;
using System.Linq;

namespace Install
{
    public class StaticDriveConfig
    {
        public DriveInfo EfiespDrive { get; }
        public DriveInfo DataDrive { get; }
        public string BcdFileName { get; }

        private StaticDriveConfig(DriveInfo efiespDrive, DriveInfo dataDrive)
        {
            EfiespDrive = efiespDrive;
            DataDrive = dataDrive;
            BcdFileName = Path.Combine(EfiespDrive.RootDirectory.Name, "EFI", "Microsoft", "Boot", "BCD");
        }

        public static StaticDriveConfig Create()
        {
            var drives = DriveInfo.GetDrives();

            try
            {
                var efiEsp = drives.First(x => x.DriveFormat == "FAT" && x.VolumeLabel == "EFIESP");
                var data = drives.First(x => x.DriveFormat == "NTFS" && x.VolumeLabel == "Data");
                return new StaticDriveConfig(efiEsp, data);
            }
            catch (InvalidOperationException e)
            {
                throw new InvalidOperationException("Cannot access Phone partitions");
            }            
        }
    }
}