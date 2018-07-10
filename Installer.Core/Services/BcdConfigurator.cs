using System;
using Installer.Core.FileSystem;
using Installer.Core.Utils;

namespace Installer.Core.Services
{
    public class BcdConfigurator
    {
        private readonly BcdInvoker invoker;
        private readonly Volume efiespVolume;
        private readonly string bcdEdit;

        public BcdConfigurator(BcdInvoker invoker, Volume efiespVolume)
        {
            this.invoker = invoker;
            this.efiespVolume = efiespVolume;
            bcdEdit = @"c:\Windows\SysNative\bcdedit.exe";
        }

        public void SetupBcd()
        {
            var bootShimEntry = CreateBootShim();
            SetupBootShim(bootShimEntry);
            SetupBootMgr();
            SetDisplayOptions(bootShimEntry);
        }

        private void SetDisplayOptions(Guid entry)
        {
            invoker.Invoke($@"/displayorder {{{entry}}}");
            invoker.Invoke($@"/default {{{entry}}}");
            invoker.Invoke($@"/timeout 30");
        }

        private void SetupBootShim(Guid guid)
        {
            invoker.Invoke($@"/set {{{guid}}} path \EFI\boot\BootShim.efi");
            invoker.Invoke($@"/set {{{guid}}} device partition={efiespVolume.RootDir.Name}");
            invoker.Invoke($@"/set {{{guid}}} testsigning on");
            invoker.Invoke($@"/set {{{guid}}} nointegritychecks on");
        }

        private void SetupBootMgr()
        {
            invoker.Invoke($@"/set {{bootmgr}} displaybootmenu on");
            invoker.Invoke($@"/deletevalue {{bootmgr}} customactions");
            invoker.Invoke($@"/deletevalue {{bootmgr}} custom:54000001");
            invoker.Invoke($@"/deletevalue {{bootmgr}} custom:54000002");
        }
        
        private Guid CreateBootShim()
        {
            var run = ProcessUtils.Run(bcdEdit, $@"/STORE {efiespVolume.GetBcdFullFilename()} /create /d ""Windows 10"" /application BOOTAPP");
            return FormattingUtils.GetGuid(run);
        }
    }
}