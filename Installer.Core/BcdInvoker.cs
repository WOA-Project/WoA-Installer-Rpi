namespace Installer.Core
{
    public class BcdInvoker
    {
        private readonly string commonArgs;
        private readonly string bcdEdit;

        public BcdInvoker(Config config)
        {
            bcdEdit = @"c:\Windows\SysNative\bcdedit.exe";
            commonArgs = $"/STORE {config.BcdFileName}";
        }

        public string Invoke(string command)
        {
            return CmdUtils.Run(bcdEdit, $@"{commonArgs} {command}");
        }
    }
}