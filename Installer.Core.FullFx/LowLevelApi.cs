using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Threading.Tasks;
using Microsoft.PowerShell.Cim;

namespace Installer.Core.FullFx
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

            var results = await Task.Factory.FromAsync(ps.BeginInvoke(), r => ps.EndInvoke(r));
            var disk = results.First().ImmediateBaseObject;
            var diskNumber = (uint)adapter.GetPropertyValue(adapter.GetProperty(disk, "Number"));

            return new Disk(diskNumber);
        }

        public Task EnsurePartitionMounted(string label, string filesystemType)
        {
            ps.Commands.Clear();
            ps.AddCommand("EnsurePartitionMountedForVolume")
                .AddParameter("label", label)
                .AddParameter("fileSystemType", filesystemType);

            return Task.Factory.FromAsync(ps.BeginInvoke(), x => ps.EndInvoke(x));
        }

        public Task RemoveExistingWindowsPartitions()
        {
            ps.Commands.Clear();
            ps.AddCommand("RemoveExistingWindowsPartitions");

            return Task.Factory.FromAsync(ps.BeginInvoke(), x => ps.EndInvoke(x));
        }

        public async Task<double> GetAvailableFreeSpace(Disk disk)
        {
            ps.Commands.Clear();
            ps.AddCommand("GetAvailableSpace")
                .AddParameter("diskNumber", disk.Number);

            var results = await Task.Factory.FromAsync(ps.BeginInvoke(), r => ps.EndInvoke(r));
            var space = Convert.ToDouble(results.First().ImmediateBaseObject);
            return space;
        }


        public Task ResizePartition(Partition partition, long sizeInBytes)
        {
            ps.Commands.Clear();
            ps.AddCommand("ResizePartition")
                .AddParameter("diskNumber", partition.Disk.Number)
                .AddParameter("partitionNumber", partition.Number)
                .AddParameter("sizeInBytes", sizeInBytes);

            return Task.Factory.FromAsync(ps.BeginInvoke(), x => ps.EndInvoke(x));
        }

        public async Task<List<Partition>> GetPartitions(Disk disk)
        {
            ps.Commands.Clear();
            ps.AddCommand("GetPartitions")
                .AddParameter("diskNumber", disk.Number);

            var results = await Task.Factory.FromAsync(ps.BeginInvoke(), x => ps.EndInvoke(x));

            var volumes = results
                .Select(x => x.ImmediateBaseObject)
                .Select(x => new Partition
                {
                    Disk = disk,
                    Number = (uint)x.GetValue("PartitionNumber"),
                    Id = (string)x.GetValue("UniqueId"),
                });

            return volumes.ToList();
        }

        public async Task<Volume> GetVolume(Partition partition)
        {
            ps.Commands.Clear();
            ps.AddCommand("GetVolume")
                .AddParameter("partitionId", partition.Id);

            var results = await Task.Factory.FromAsync(ps.BeginInvoke(), x => ps.EndInvoke(x));
            var volume = results.First().ImmediateBaseObject;

            return new Volume()
            {
                Partition = partition,
                Label = (string)volume.GetValue("FileSystemLabel"),
                Size = Convert.ToUInt32(volume.GetValue("Size"))
            };
        }
    }
}