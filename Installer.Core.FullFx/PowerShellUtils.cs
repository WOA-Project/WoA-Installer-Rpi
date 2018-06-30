using Microsoft.PowerShell.Cim;

namespace Installer.Core.FullFx
{
    public static class PowerShellUtils
    {
        private static readonly CimInstanceAdapter adapter = new CimInstanceAdapter();

        public static object GetValue(this object obj, string propertyName)
        {
            return adapter.GetPropertyValue(adapter.GetProperty(obj, propertyName));
        }
    }
}