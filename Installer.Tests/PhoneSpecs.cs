using System.Threading.Tasks;
using Installer.Core.FullFx;
using Installer.Lumia.Core;
using Xunit;

namespace Application.Tests
{
    public class PhoneSpecs
    {
        [Fact]
        public async Task GetDualBootStatus()
        {
            var sut = await Phone.Load(new LowLevelApi());
            var status = await sut.GetDualBootStatus();
        }
    }
}