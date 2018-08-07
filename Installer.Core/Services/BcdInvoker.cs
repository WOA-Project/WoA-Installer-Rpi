using Installer.Core.Utils;

namespace Installer.Core.Services
{
    public class BcdInvoker
    {
        private readonly string commonArgs;
        private readonly string bcdEdit;

        public BcdInvoker(string store)
        {
            bcdEdit = SystemPaths.BcdEdit;
            commonArgs = $@"/STORE ""{store}""";
        }

        public string Invoke(string command)
        {
            return ProcessUtils.Run(bcdEdit, $@"{commonArgs} {command}");
        }
    }
}