using System.Threading.Tasks;
using Install;
using Xunit;

namespace Application.Tests
{
    
    public class LowLevelApiSpecs
    {
        [Fact]
        public async Task GetPhoneDisk()
        {
            var sut = new LowLevelApi();
            var disk = await sut.GetPhoneDisk();
            Assert.NotNull(disk);
        }

        [Fact]
        public async Task EnsurePartitionsAreMounted()
        {
            var sut = new LowLevelApi();
            await sut.EnsurePartitionsAreMounted();
        }
    }
}
