using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Threading.Tasks;
using Microsoft.PowerShell.Cim;

namespace Install
{
    public class LowLevelApi : ILowLevelApi
    {
        private readonly PowerShell ps;
        private readonly CimInstanceAdapter adapter;


        public LowLevelApi()
        {
            ps = PowerShell.Create();
            ps.AddScript(File.ReadAllText("Functions.ps1"));
            ps.Invoke();
            adapter = new CimInstanceAdapter();
        }

        public Volume GetVolume(string label, string fileSystemFormat)
        {
          
            ps.Commands.Clear();
            ps.AddCommand("GetVolume")
                .AddParameter("label", "Sistema")
                .AddParameter("fileSystemType", "NTFS");

            return new Volume();
        }

        public async Task<Disk> GetPhoneDisk()
        {
            ps.Commands.Clear();
            ps.AddCommand("GetPhoneDisk");

            var result = await Task.Factory.FromAsync(ps.BeginInvoke(), r => ps.EndInvoke(r));
            var disk = result.First().ImmediateBaseObject;
            var diskNumber = (uint) adapter.GetPropertyValue(adapter.GetProperty(disk, "Number"));

            return new Disk(diskNumber);
        }

        public async Task EnsurePartitionsAreMounted()
        {
            ps.Commands.Clear();
            ps.AddCommand("EnsurePartitionsAreMounted");

            await Task.Factory.FromAsync(ps.BeginInvoke(), x => ps.EndInvoke(x));
            ps.Invoke();
        }
    }
}