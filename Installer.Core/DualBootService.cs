using System;
using System.Threading;
using System.Threading.Tasks;

namespace Installer.Core
{
    public class DualBootService
    {
        private readonly IConfigProvider provider;
        private readonly ILowLevelApi api;

        public DualBootService(IConfigProvider provider, ILowLevelApi api)
        {
            this.provider = provider;
            this.api = api;
        }

        public async Task<bool> IsDualBootCapabable()
        {
            var config = await provider.Retrieve();
            var disk = config.PhoneDisk;

            throw new NotImplementedException();
        }
    }
}