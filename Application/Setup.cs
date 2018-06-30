using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Serilog;

namespace Install
{
    public class Setup : ISetup
    {
        private readonly StaticDriveConfig config;
        private readonly LowLevelApi lowLevelApi;
        private readonly BcdInvoker bcdInvoker;

        public Setup()
        {
            config = StaticDriveConfig.Create();
            lowLevelApi = new LowLevelApi();   
            bcdInvoker = new BcdInvoker(config);
        }
       
        public async Task FullInstall(InstallOptions options)
        {
            await PerformSanityCheck();
            await DeployUefi(config);
            new BcdConfigurator(config, bcdInvoker).SetupBcd();
            await AddDeveloperMenu();
            await new WindowsDeployment().Execute();
        }

        private async Task AddDeveloperMenu()
        {
            var destination = Path.Combine(config.EfiespDrive.RootDirectory.Name, "Windows", "System32", "BOOT");
            await FileUtils.CopyDirectory(new DirectoryInfo(Path.Combine("Files", "Developer Menu")), new DirectoryInfo(destination));
            var guid = FormattingUtils.GetGuid(bcdInvoker.Invoke(@"/create /d ""Developer Menu"" /application BOOTAPP"));
            bcdInvoker.Invoke($@"/set {{{guid}}} path \Windows\System32\BOOT\developermenu.efi");
            var driveLetter = config.EfiespDrive.RootDirectory.Name;
            bcdInvoker.Invoke($@"/set {{{guid}}} device partition={driveLetter}");
            bcdInvoker.Invoke($@"/displayorder {{{guid}}} /addlast");
        }

        private async Task PerformSanityCheck()
        {
            Log.Information("Checking partitions...");
            await lowLevelApi.EnsurePartitionsAreMounted();
        }
        
        private async Task DeployUefi(StaticDriveConfig config)
        {
            await FileUtils.Copy(Path.Combine("Files", "Core", "UEFI.elf"), Path.Combine(config.EfiespDrive.RootDirectory.Name, "UEFI.elf"));
            await FileUtils.Copy(Path.Combine("Files", "Core", "emmc_appsboot.mbn"), Path.Combine(config.EfiespDrive.RootDirectory.Name, "emmc_appsboot.mbn"));
            await FileUtils.Copy(Path.Combine("Files", "Core", "BootShim.efi"), Path.Combine(config.EfiespDrive.RootDirectory.Name, "EFI", "BOOT", "BootShim.efi"));
        }
    }
}