using System;
using System.Threading.Tasks;

namespace Install
{
    public class WindowsDeployment
    {
        private const float totalGBneeded = 18;

        public async Task Execute()
        {
            await AllocateSpace();
            await CreatePartitions();
            await DeployWindows();
            await InjectBasicDrivers();            
        }

        private Task InjectBasicDrivers()
        {
            throw new NotImplementedException();
        }

        private Task DeployWindows()
        {
            throw new NotImplementedException();
        }

        private Task CreatePartitions()
        {
            throw new NotImplementedException();
        }

        private Task AllocateSpace()
        {
            throw new NotImplementedException();
        }
    }
}