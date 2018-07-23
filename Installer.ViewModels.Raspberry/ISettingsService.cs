namespace Installer.Raspberry.ViewModels
{
    public interface ISettingsService
    {
        string DriverPackFolder { get; set; }
        string WimFolder { get; set; }
    }
}