using System;
using System.IO;
using System.Threading.Tasks;
using Serilog;

namespace Installer.Core
{
    public class Setup : ISetup
    {
        private readonly ILowLevelApi lowLevelApi;
        private readonly IWindowsImageService imageService;
        private readonly ConfigProvider configProvider;

        public Setup(ILowLevelApi lowLevelApi, IWindowsImageService imageService)
        {
            this.lowLevelApi = lowLevelApi;
            this.imageService = imageService;
            configProvider = new ConfigProvider(lowLevelApi);
        }

        public async Task DeployUefiAndWindows(InstallOptions options, IObserver<double> progressObserver)
        {
            EnsureValidFilesRepository();

            Log.Information("Retrieving information from Phone Disk/partitions...");

            var config = await configProvider.Retrieve();
            await DeployUefi(config);
            var bcdInvoker = new BcdInvoker(config.BcdFileName);
            new BcdConfigurator(config, bcdInvoker).SetupBcd();
            await AddDeveloperMenu(config, bcdInvoker);
            await new WindowsDeployer(lowLevelApi, configProvider, imageService).Deploy(options.ImagePath, options.ImageIndex, progressObserver);

            Log.Information("Full installation complete!");
        }

        private void EnsureValidFilesRepository()
        {
            Log.Information(@"Ensuring we have a correct Files repository under ""Files folder""");

            var paths = new[]
            {
                @"Core\BootShim.efi",
                @"Core\UEFI.elf",
                @"Drivers",
                @"Developer Menu"
            };

            foreach (var path in paths)
            {
                Log.Verbose("Testing path {Path}", path);
                if (!FileUtils.TestPath(Path.Combine("Files", path)))
                {
                    throw new InvalidFileRepositoryException(Resources.EnsureValidFilesRepository);
                }
            }

            Log.Information(@"Files folder seems to be valid");

        }

        public async Task DeployWindows(InstallOptions options, IObserver<double> progressObserver)
        {
            EnsureValidFilesRepository();

            await new WindowsDeployer(lowLevelApi, configProvider, imageService).Deploy(options.ImagePath, options.ImageIndex, progressObserver);

            Log.Information("Windows deployment succeeded!");
        }

        private async Task AddDeveloperMenu(Config config, BcdInvoker bcdInvoker)
        {
            Log.Information("Adding Development Menu...");

            var destination = Path.Combine(config.EfiespDrive.RootDirectory.Name, "Windows", "System32", "BOOT");
            await FileUtils.CopyDirectory(new DirectoryInfo(Path.Combine("Files", "Developer Menu")), new DirectoryInfo(destination));
            var guid = FormattingUtils.GetGuid(bcdInvoker.Invoke(@"/create /d ""Developer Menu"" /application BOOTAPP"));
            bcdInvoker.Invoke($@"/set {{{guid}}} path \Windows\System32\BOOT\developermenu.efi");
            var partition = config.EfiespDrive.RootDirectory.Name;
            bcdInvoker.Invoke($@"/set {{{guid}}} device partition={partition}");
            bcdInvoker.Invoke($@"/displayorder {{{guid}}} /addlast");
        }

        private async Task DeployUefi(Config config)
        {
            Log.Information("Deploying UEFI...");

            await FileUtils.Copy(Path.Combine("Files", "Core", "UEFI.elf"), Path.Combine(config.EfiespDrive.RootDirectory.Name, "UEFI.elf"));
            await FileUtils.Copy(Path.Combine("Files", "Core", "emmc_appsboot.mbn"), Path.Combine(config.EfiespDrive.RootDirectory.Name, "emmc_appsboot.mbn"));
            await FileUtils.Copy(Path.Combine("Files", "Core", "BootShim.efi"), Path.Combine(config.EfiespDrive.RootDirectory.Name, "EFI", "BOOT", "BootShim.efi"));
        }
    }
}