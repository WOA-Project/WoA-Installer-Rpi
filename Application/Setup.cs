using System.Threading.Tasks;
using Serilog;

namespace Install
{
    public class Setup : ISetup
    {
        private readonly ILowLevelApi lowLevelApi;

        public Setup(ILowLevelApi lowLevelApi)
        {
            this.lowLevelApi = lowLevelApi;
        }

        public async Task FullInstall(InstallOptions options)
        {
            await PerformSanityCheck();
            await BasicInstall();
            Log.Information("Done!");
        }

        private async Task PerformSanityCheck()
        {
            Log.Information("Checking partitions...");
            await lowLevelApi.EnsurePartitionsAreMounted();
        }

        private async Task BasicInstall()
        {
            await DeployUefi();
            await SetupBcd();
            await CreateDeveloperMenu();
            await InstallWindows();
            await EnableDualBoot();
        }

        private Task EnableDualBoot()
        {
            return Task.CompletedTask;
        }

        private Task InstallWindows()
        {
            return Task.CompletedTask;
        }

        private Task CreateDeveloperMenu()
        {
            return Task.CompletedTask;        }

        private Task SetupBcd()
        {
            return Task.CompletedTask;
        }

        private Task DeployUefi()
        {
            return Task.CompletedTask;
        }
    }
}