using Install;
using Xunit;

namespace UnitTestProject1
{
    public class UnitTest1
    {
        [Fact]
        public void GetVolume()
        {
            var sut = new LowLevelApi();
            var volume  = sut.GetVolume("Sistema", "NTFS");
        }     
        
        [Fact]
        public void GetPhoneDisk()
        {
            var sut = new LowLevelApi();
            var disk  = sut.GetPhoneDisk();
        }
    }
}
