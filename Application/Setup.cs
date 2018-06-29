using System.Threading.Tasks;
using Serilog;
using Serilog.Core;
using Serilog.Events;

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
            Log.Information("Checking partitions...");
            await lowLevelApi.EnsurePartitionsAreMounted();
            await BasicInstall();
            Log.Information("Done!");
        }

        private Task BasicInstall()
        {
            return Task.CompletedTask;
        }
    }
}