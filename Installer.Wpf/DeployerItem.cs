using Installer.Core;

namespace Intaller.Wpf
{
    public class DeployerItem
    {
        public PhoneModel Model { get; }
        public IDeployer Deployer { get; }

        public DeployerItem(PhoneModel model, IDeployer deployer)
        {
            Model = model;
            Deployer = deployer;
        }
    }
}