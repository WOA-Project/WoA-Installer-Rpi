namespace Install
{
    public class BcdInvoker
    {
        private readonly StaticDriveConfig config;
        private readonly string commonArgs;
        private readonly string bcdEdit;

        public BcdInvoker(StaticDriveConfig config)
        {
            this.config = config;
            bcdEdit = @"c:\Windows\SysNative\bcdedit.exe";
            commonArgs = $"/STORE {config.BcdFileName}";
        }

        public string Invoke(string command)
        {
            return CmdUtils.Run(bcdEdit, $@"{commonArgs} {command}");
        }
    }
}