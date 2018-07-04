using System;
using System.IO;
using System.Reactive.Linq;
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

        public async Task FullInstall(InstallOptions options, IObserver<double> progressObserver)
        {
            Log.Information("Retrieving information from Phone Disk/partitions...");

            var config = await configProvider.Retrieve();
            await DeployUefi(config);
            var bcdInvoker = new BcdInvoker(config.BcdFileName);
            new BcdConfigurator(config, bcdInvoker).SetupBcd();
            await AddDeveloperMenu(config, bcdInvoker);
            await new WindowsDeployer(lowLevelApi, configProvider, imageService).Deploy(options.ImagePath, options.ImageIndex, progressObserver);
        }

        public async Task WindowsInstall(InstallOptions options, IObserver<double> progressObserver)
        {
            await new WindowsDeployer(lowLevelApi, configProvider, imageService).Deploy(options.ImagePath, options.ImageIndex, progressObserver);
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