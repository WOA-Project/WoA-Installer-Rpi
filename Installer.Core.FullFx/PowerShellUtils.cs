using Microsoft.PowerShell.Cim;

namespace Installer.Core.FullFx
{
    public static class PowerShellUtils
    {
        private static readonly CimInstanceAdapter adapter = new CimInstanceAdapter();

        public static object GetPropertyValue(this object obj, string propertyName)
        {
            var psAdaptedProperty = adapter.GetProperty(obj, propertyName);
            if (psAdaptedProperty == null)
            {
                return null;
            }

            return adapter.GetPropertyValue(psAdaptedProperty);
        }
    }
}