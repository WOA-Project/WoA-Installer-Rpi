using Installer.Core.Services;

namespace Installer.Core.FullFx
{
    public class DefaultServiceFactory : ServiceFactory
    {
        public DefaultServiceFactory()
        {
            DiskService = new DiskService(new LowLevelApi());
            ImageService = new DismImageService();
        }
    }
}