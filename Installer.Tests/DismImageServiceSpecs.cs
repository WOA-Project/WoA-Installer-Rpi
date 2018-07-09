using System.Linq;
using System.Threading.Tasks;
using Installer.Core.FullFx;
using Xunit;

namespace Application.Tests
{
    public class DismImageServiceSpecs
    {
        [Fact]
        public async Task InjectDrivers()
        {
            var sut = new DismImageService();
            var lowlevel = new LowLevelApi();
            var volumes = await lowlevel.GetVolumes(await lowlevel.GetPhoneDisk());
            var winVolume = volumes.Single(v => v.Label == "WindowsARM");

            await sut.InjectDrivers(@"C:\Users\super\source\repos\Install\Application\bin\Debug\bin\Debug\Files\Drivers\Stable", winVolume);
        }
    }
}