using System.Linq;
using System.Threading.Tasks;
using Installer.Core.FullFx;
using Installer.Core.Services;
using Installer.Lumia.Core;
using Xunit;

namespace Application.Tests
{
    public class WindowsDeployerTests
    {
        [Fact]
        public async Task MakeBootable()
        {
            var lowLevelApi = new LowLevelApi();
            var sut = new LumiaWindowsDeployer(new DismImageService(), new DriverPaths(""));

            var lowlevel = lowLevelApi;
            var volumes = await lowlevel.GetVolumes(await lowlevel.GetPhoneDisk());
            var winVolume = volumes.Single(v => v.Label == "WindowsARM");
            var bootVolume = volumes.Single(v => v.Label == "BOOT");

            //await sut.MakeBootable(new WindowsVolumes());
        }
    }
}