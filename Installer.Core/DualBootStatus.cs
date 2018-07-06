namespace Installer.Core
{
    public class DualBootStatus
    {
        public DualBootStatus(bool isEnabled, bool canDualBoot)
        {
            IsEnabled = isEnabled;
            CanDualBoot = canDualBoot;
        }

        public bool IsEnabled { get; }
        public bool CanDualBoot { get; }
    }
}