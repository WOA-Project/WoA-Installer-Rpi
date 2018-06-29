using System.Threading.Tasks;

namespace Install
{
    public interface ILowLevelApi
    {
        Volume GetVolume(string label, string fileSystemFormat);
        Task<Disk> GetPhoneDisk();
        Task EnsurePartitionsAreMounted();
    }
}