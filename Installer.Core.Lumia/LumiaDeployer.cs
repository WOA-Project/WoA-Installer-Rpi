using System;
using System.Threading.Tasks;
using Installer.Core;
using Installer.Core.Exceptions;
using Installer.Core.Services;
using Serilog;

namespace Installer.Lumia.Core
{
    public class LumiaDeployer : IDeployer<Phone>
    {
        private readonly ICoreDeployer<Phone> coreDeployer;
        private readonly IWindowsDeployer<Phone> windowsDeployer;

        public LumiaDeployer(ICoreDeployer<Phone> coreDeployer, IWindowsDeployer<Phone> windowsDeployer)
        {
            this.coreDeployer = coreDeployer;
            this.windowsDeployer = windowsDeployer;
        }

        public async Task DeployCoreAndWindows(InstallOptions options, Phone phone, IObserver<double> progressObserver = null)
        {
            await EnsureValidCoreWindowsDeployment();

            Log.Information("Deploying Core And Windows 10 ARM64...");

            await coreDeployer.Deploy(phone);
            await windowsDeployer.Deploy(options, phone, progressObserver);

            Log.Information("Deployment successful");
        }

        public async Task DeployWindows(InstallOptions options, Phone phone, IObserver<double> progressObserver = null)
        {
            await EnsureValidWindowsDeployment();

            Log.Information("Deploying Windows 10 ARM64...");

            await EnsureNoDualBootMenu(phone);
            await windowsDeployer.Deploy(options, phone, progressObserver);

            Log.Information("Deployment successful");
        }

        private static async Task EnsureNoDualBootMenu(Phone phone)
        {
            Log.Information("Ensuring that Dual Boot option is removed from the Boot Menu. You may enable Dual Boot after Windows Setup...");
            await phone.RemoveWindowsPhoneBcdEntry();
        }

        private async Task EnsureValidWindowsDeployment()
        {
            await EnsureWindowsFiles();
        }

        private async Task EnsureValidCoreWindowsDeployment()
        {
            await EnsureCoreFiles();
            await EnsureWindowsFiles();
        }

        private async Task EnsureCoreFiles()
        {
            Log.Verbose("Checking Core Deployment Files...");

            var areValid = await coreDeployer.AreDeploymentFilesValid();
            if (!areValid)
            {
                throw new InvalidDeploymentRepositoryException("The Files repository isn't valid. Please, check that you've installed a valid Core Package");
            }
        }

        private async Task EnsureWindowsFiles()
        {
            Log.Verbose("Checking Windows Deployment Files...");

            var areValid = await windowsDeployer.AreDeploymentFilesValid();
            if (!areValid)
            {
                throw new InvalidDeploymentRepositoryException("The Files repository doesn't contain the required files. Please, check that you've installed a valid Core Package");
            }
        }

        public async Task InjectPostOobeDrivers(Phone phone)
        {
            Log.Information("Injecting Post-OOBE drivers...");

            await windowsDeployer.InjectPostOobeDrivers(phone);

            Log.Information("Injection of drivers successful");
        }
    }
}