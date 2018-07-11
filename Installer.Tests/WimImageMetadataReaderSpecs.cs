using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Installer.Core.FullFx;
using Installer.Core.Services.Wim;
using Xunit;

namespace Application.Tests
{
    public class WimImageMetadataReaderSpecs
    {
        [Fact]
        public void TestXml()
        {
           var sut = new XmlWindowsImageMetadataReader();
            using (var s = File.OpenRead("test.xml"))
            {
                var metadata = sut.Load(s);
            }
        }
    }
}