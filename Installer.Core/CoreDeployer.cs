using System.IO;
using System.Threading.Tasks;
using Installer.Core.Exceptions;
using Installer.Core.FileSystem;
using Installer.Core.Services;
using Installer.Core.Utils;
using Serilog;

namespace Installer.Core
{
    public class CoreDeployer : ICoreDeployer
    {
        private readonly string rootFilesPath;

        public CoreDeployer(string rootFilesPath)
        {
            this.rootFilesPath = rootFilesPath;
        }
        
        private string[] FilesPathsToCheck => new[]
        {
            Path.Combine(rootFilesPath, "Core", "BootShim.efi"),
            Path.Combine(rootFilesPath, "Core", "UEFI.elf"),
            Path.Combine(rootFilesPath, "Developer Menu")
        };
          
        private async Task AddDeveloperMenu(Volume efiespVolume)
        {
            var bcdInvoker = new BcdInvoker(efiespVolume.GetBcdFullFilename());
            new BcdConfigurator(bcdInvoker, efiespVolume).SetupBcd();
            Log.Information("Adding Development Menu...");

            var rootDir = efiespVolume.RootDir.Name;

            var destination = Path.Combine(rootDir, "Windows", "System32", "BOOT");
            await FileUtils.CopyDirectory(new DirectoryInfo(Path.Combine(rootFilesPath, "Developer Menu")),
                new DirectoryInfo(destination));
            var guid = FormattingUtils.GetGuid(
                bcdInvoker.Invoke(@"/create /d ""Developer Menu"" /application BOOTAPP"));
            bcdInvoker.Invoke($@"/set {{{guid}}} path \Windows\System32\BOOT\developermenu.efi");
            bcdInvoker.Invoke($@"/set {{{guid}}} device partition={rootDir}");
            bcdInvoker.Invoke($@"/set {{{guid}}} testsigning on");
            bcdInvoker.Invoke($@"/set {{{guid}}} nointegritychecks on");
            bcdInvoker.Invoke($@"/displayorder {{{guid}}} /addlast");
        }
        
        private async Task DeployUefi(Volume efiespVolume)
        {
            Log.Information("Deploying UEFI...");

            var rootDir = efiespVolume.RootDir.Name;
            await FileUtils.Copy(Path.Combine(rootFilesPath, "Core", "UEFI.elf"), Path.Combine(rootDir, "UEFI.elf"));
            await FileUtils.Copy(Path.Combine(rootFilesPath, "Core", "emmc_appsboot.mbn"),
                Path.Combine(rootDir, "emmc_appsboot.mbn"));
            await FileUtils.Copy(Path.Combine(rootFilesPath, "Core", "BootShim.efi"),
                Path.Combine(rootDir, "EFI", "BOOT", "BootShim.efi"));
        }

        public Task<bool> AreDeploymentFilesValid()
        {
            var valid = FilesPathsToCheck.EnsureExistingPaths();
            return Task.FromResult(valid);
        }

        public async Task Deploy(Phone phone)
        {
            var efiespVolume = await phone.GetEfiespVolume();

            Log.Information("Deploying Core (UEFI and Development Men)");

            await DeployUefi(efiespVolume);
            await AddDeveloperMenu(efiespVolume);

            Log.Information("Core deployed (UEFI and Development Menu)");
        }
    }
}