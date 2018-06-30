using System;

namespace Install
{
    public class BcdConfigurator
    {
        private readonly StaticDriveConfig config;
        private readonly BcdInvoker invoker;
        private readonly string bcdEdit;

        public BcdConfigurator(StaticDriveConfig config, BcdInvoker invoker)
        {
            this.config = config;
            this.invoker = invoker;
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
            invoker.Invoke($@"/set {{{guid}}} device partition={config.EfiespDrive.RootDirectory.Name}");
            invoker.Invoke($@"/set {{{guid}}} testsigning on");
            invoker.Invoke($@"/set {{{guid}}} nointegritychecks on");
        }

        private void SetupBootMgr()
        {
            invoker.Invoke($@"/set {{bootmgr}} displaybootmenu on");
            invoker.Invoke($@"/deletevalue {{bootmgr}} custom:54000001");
            invoker.Invoke($@"/deletevalue {{bootmgr}} custom:54000002");
        }
        
        private Guid CreateBootShim()
        {
            return FormattingUtils.GetGuid(CmdUtils.Run(bcdEdit, $@"/STORE {config.BcdFileName} /create /d ""Windows 10"" /application BOOTAPP"));
        }
    }
}