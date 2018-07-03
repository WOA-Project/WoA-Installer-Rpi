namespace Installer.Core
{
    public class BcdInvoker2
    {
        private readonly string commonArgs;
        private readonly string bcdEdit;

        public BcdInvoker2(string store)
        {
            bcdEdit = @"c:\Windows\SysNative\bcdedit.exe";
            commonArgs = $"/STORE {store}";
        }

        public string Invoke(string command)
        {
            return CmdUtils.Run(bcdEdit, $@"{commonArgs} {command}");
        }
    }
}