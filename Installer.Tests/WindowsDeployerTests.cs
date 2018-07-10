using System.Linq;
using System.Threading.Tasks;
using Installer.Core;
using Installer.Core.FullFx;
using Installer.Core.Services;
using Xunit;

namespace Application.Tests
{
    public class WindowsDeployerTests
    {
        [Fact]
        public async Task MakeBootable()
        {
            var lowLevelApi = new LowLevelApi();
            var sut = new WindowsDeployer(new DismImageService(), new Phone(await lowLevelApi.GetPhoneDisk()));

            var lowlevel = lowLevelApi;
            var volumes = await lowlevel.GetVolumes(await lowlevel.GetPhoneDisk());
            var winVolume = volumes.Single(v => v.Label == "WindowsARM");
            var bootVolume = volumes.Single(v => v.Label == "BOOT");

            await sut.MakeBootable(new WindowsDeployer.WindowsVolumes(bootVolume, winVolume));
        }
    }
}