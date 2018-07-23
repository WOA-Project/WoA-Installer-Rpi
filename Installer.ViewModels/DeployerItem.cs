using Installer.Core;
using Installer.Lumia.Core;

namespace Installer.Lumia.ViewModels
{
    public class DeployerItem
    {
        public PhoneModel Model { get; }
        public IDeployer<Phone> Deployer { get; }

        public DeployerItem(PhoneModel model, IDeployer<Phone> deployer)
        {
            Model = model;
            Deployer = deployer;
        }
    }
}