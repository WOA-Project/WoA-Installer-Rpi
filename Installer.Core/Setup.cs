using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Serilog;
using SharpCompress.Archives.SevenZip;

namespace Installer.Core
{
    public class Setup : ISetup
    {
        private readonly ILowLevelApi lowLevelApi;
        private readonly IWindowsImageService imageService;
        private readonly IDriverPackageImporter packageImporter;

        public Setup(ILowLevelApi lowLevelApi, IWindowsImageService imageService, IDriverPackageImporter packageImporter)
        {
            this.lowLevelApi = lowLevelApi;
            this.imageService = imageService;
            this.packageImporter = packageImporter;
        }

        public async Task DeployUefiAndWindows(InstallOptions options, IObserver<double> progressObserver = null)
        {
            EnsureValidFilesRepository();

            var disk = await lowLevelApi.GetPhoneDisk();
            var phone = new Phone(disk);
            var efiespVolume = await phone.GetEfiespVolume();

            Log.Information("Retrieving information from Phone Disk/partitions...");

            await DeployUefi(efiespVolume);
            var bcdInvoker = new BcdInvoker(efiespVolume.GetBcdFullFilename());
            new BcdConfigurator(bcdInvoker, efiespVolume).SetupBcd();
            await AddDeveloperMenu(bcdInvoker, efiespVolume);
            await new WindowsDeployer(lowLevelApi, imageService, phone).Deploy(options.ImagePath, options.ImageIndex, progressObserver);

            Log.Information("Full installation complete!");
        }

        private void EnsureValidFilesRepository()
        {
            Log.Information(@"Ensuring we have a correct Files repository under ""Files folder""");

            var paths = new[]
            {
                Path.Combine("Core", "BootShim.efi"),
                Path.Combine("Core", "UEFI.elf"),
                Path.Combine("Drivers", "Pre-OOBE"),
                Path.Combine("Drivers", "Post-OOBE"),
                "Developer Menu",
            };

            foreach (var path in paths)
            {
                Log.Verbose("Testing path {Path}", path);
                if (!FileUtils.TestPath(Path.Combine("Files", path)))
                {
                    throw new InvalidRepositoryException(Resources.EnsureValidFilesRepository);
                }
            }

            Log.Information(@"Files folder seems to be valid");

        }

        public async Task DeployWindows(InstallOptions options, IObserver<double> progressObserver)
        {
            EnsureValidFilesRepository();

            var disk = await lowLevelApi.GetPhoneDisk();
            var phone = new Phone(disk);

            await new WindowsDeployer(lowLevelApi, imageService, phone).Deploy(options.ImagePath, options.ImageIndex, progressObserver);

            Log.Information("Windows deployment succeeded!");
        }

        public async Task InjectPostOobeDrivers()
        {
            var disk = await lowLevelApi.GetPhoneDisk();
            var phone = new Phone(disk);
            await new WindowsDeployer(lowLevelApi, imageService, phone).InjectPostOobeDrivers();
        }

        public async Task InstallDriverPackage(string fileName, IObserver<double> progressObserver = null)
        {
            await packageImporter.ImportDriverPackage(fileName, @"Files\Drivers", progressObserver);
        }

        public async Task<string> GetDriverPackageReadmeText(string fileName)
        {
            return await packageImporter.GetReadmeText(fileName);
        }

        private async Task AddDeveloperMenu(BcdInvoker bcdInvoker, Volume efiespVolume)
        {
            Log.Information("Adding Development Menu...");

            var rootDir = efiespVolume.RootDir.Name;

            var destination = Path.Combine(rootDir, "Windows", "System32", "BOOT");
            await FileUtils.CopyDirectory(new DirectoryInfo(Path.Combine("Files", "Developer Menu")), new DirectoryInfo(destination));
            var guid = FormattingUtils.GetGuid(bcdInvoker.Invoke(@"/create /d ""Developer Menu"" /application BOOTAPP"));
            bcdInvoker.Invoke($@"/set {{{guid}}} path \Windows\System32\BOOT\developermenu.efi");
            bcdInvoker.Invoke($@"/set {{{guid}}} device partition={rootDir}");
            bcdInvoker.Invoke($@"/displayorder {{{guid}}} /addlast");
        }

        private async Task DeployUefi(Volume efiespVolume)
        {
            Log.Information("Deploying UEFI...");

            var rootDir = efiespVolume.RootDir.Name;
            await FileUtils.Copy(Path.Combine("Files", "Core", "UEFI.elf"), Path.Combine(rootDir, "UEFI.elf"));
            await FileUtils.Copy(Path.Combine("Files", "Core", "emmc_appsboot.mbn"), Path.Combine(rootDir, "emmc_appsboot.mbn"));
            await FileUtils.Copy(Path.Combine("Files", "Core", "BootShim.efi"), Path.Combine(rootDir, "EFI", "BOOT", "BootShim.efi"));
        }
    }
}