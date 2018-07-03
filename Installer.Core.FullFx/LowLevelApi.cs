using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading.Tasks;

namespace Installer.Core.FullFx
{
    public class LowLevelApi : ILowLevelApi
    {
        private readonly PowerShell ps;

        public LowLevelApi()
        {
            ps = PowerShell.Create();
            ps.AddScript(File.ReadAllText("Functions.ps1"));
            ps.Invoke();
        }

        public async Task<Disk> GetPhoneDisk()
        {
            ps.Commands.Clear();
            ps.AddCommand("GetPhoneDisk");

            var results = await Task.Factory.FromAsync(ps.BeginInvoke(), r => ps.EndInvoke(r));
            var disk = results.First().ImmediateBaseObject;

            return ToDisk(disk);
        }

        private static Disk ToDisk(object disk)
        {
            var number = (uint)disk.GetPropertyValue("Number");
            var size = (ulong)disk.GetPropertyValue("Size");
            var allocatedSize = (ulong)disk.GetPropertyValue("AllocatedSize");

            return new Disk(number, size, allocatedSize);
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


        public async Task ResizePartition(Partition partition, ulong sizeInBytes)
        {
            ps.Commands.Clear();

            ps.AddCommand("ResizePartition")
                .AddParameter("diskNumber", partition.Disk.Number)
                .AddParameter("partitionNumber", partition.Number)
                .AddParameter("sizeInBytes", sizeInBytes);

            await Task.Factory.FromAsync(ps.BeginInvoke(), x => ps.EndInvoke(x));
            if (ps.HadErrors)
            {
                throw new InvalidOperationException(@"Cannot resize the partition");
            }
        }

        public async Task<List<Partition>> GetPartitions(Disk disk)
        {
            ps.Commands.Clear();
            ps.AddCommand("GetPartitions")
                .AddParameter("diskNumber", disk.Number);

            var results = await Task.Factory.FromAsync(ps.BeginInvoke(), x => ps.EndInvoke(x));

            var volumes = results
                .Select(x => x.ImmediateBaseObject)
                .Select(x =>
                {
                    var hasType = Guid.TryParse((string) x.GetPropertyValue("GptType"), out var guid);

                    return new Partition
                    {
                        Disk = disk,
                        Number = (uint) x.GetPropertyValue("PartitionNumber"),
                        Id = (string) x.GetPropertyValue("UniqueId"),
                        Letter = (char) x.GetPropertyValue("DriveLetter"),
                        GptType = hasType ? guid : (Guid?)null,
                    };
                });

            return volumes.ToList();
        }

        public async Task<Volume> GetVolume(Partition partition)
        {
            ps.Commands.Clear();
            ps.AddScript($@"Get-Partition -UniqueId ""{partition.Id}"" | Get-Volume", true);

            var results = await Task.Factory.FromAsync(ps.BeginInvoke(), x => ps.EndInvoke(x));
            var volume = results.First().ImmediateBaseObject;

            return new Volume
            {
                Partition = partition,
                Size = Convert.ToUInt64(volume.GetPropertyValue("Size")),
                Label = (string)volume.GetPropertyValue("FileSystemLabel"),
                Letter = (char?)volume.GetPropertyValue("DriveLetter")
            };
        }

        public async Task<Partition> CreateReservedPartition(Disk disk, ulong sizeInBytes)
        {
            ps.Commands.Clear();
            ps.AddCommand("New-Partition")
                .AddParameter("DiskNumber", disk.Number)
                .AddParameter("GptType", "{e3c9e316-0b5c-4db8-817d-f92df00215ae}")
                .AddParameter("Size", sizeInBytes);

            var results = await Task.Factory.FromAsync(ps.BeginInvoke(), x => ps.EndInvoke(x));
            var partition = results.First().ImmediateBaseObject;

            return ToPartition(disk, partition);
        }

        public async Task<Partition> CreatePartition(Disk disk, ulong sizeInBytes)
        {
            ps.Commands.Clear();
            ps.AddCommand("New-Partition")
                .AddParameter("DiskNumber", disk.Number);

            if (sizeInBytes == ulong.MaxValue)
            {
                ps.AddParameter("UseMaximumSize");
            }
            else
            {
                ps.AddParameter("Size", sizeInBytes);
            }

            var results = await Task.Factory.FromAsync(ps.BeginInvoke(), x => ps.EndInvoke(x));
            var partition = results.First().ImmediateBaseObject;

            return ToPartition(disk, partition);
        }

        private static Partition ToPartition(Disk disk, object partition)
        {
            return new Partition
            {
                Disk = disk,
                Number = (uint)partition.GetPropertyValue("PartitionNumber"),
                Id = (string)partition.GetPropertyValue("UniqueId"),
                Letter = (char)partition.GetPropertyValue("DriveLetter")
            };
        }

        public Task<Partition> SetPartitionType(Partition partition, PartitionType partitionType)
        {
            throw new NotImplementedException();
        }

        public Task Format(Volume volume, FileSystemFormat fileSystemFormat, string fileSystemLabel)
        {
            ps.Commands.Clear();
            var cmd = $@"Get-Partition -UniqueId ""{volume.Partition.Id}"" | Get-Volume | Format-Volume -FileSystem {fileSystemFormat.Moniker} -NewFileSystemLabel ""{fileSystemLabel}"" -Force -Confirm:$false";
            ps.AddScript(cmd);

            return Task.Factory.FromAsync(ps.BeginInvoke(), x => ps.EndInvoke(x));
        }

        public Task AssignDriveLetter(Volume volume, char driverLetter)
        {
            ps.Commands.Clear();
            var cmd = $@"Set-Partition -DiskNumber {volume.Partition.Disk.Number} -PartitionNumber {volume.Partition.Number} -NewDriveLetter {driverLetter}";
            ps.AddScript(cmd);

            return Task.Factory.FromAsync(ps.BeginInvoke(), x => ps.EndInvoke(x));
        }

        public async Task<char> GetFreeDriveLetter()
        {
            ps.Commands.Clear();
            var cmd = $@"$normalizedName = ls function:[d-z]: -n | ?{{ !(test-path $_) }} | select -First 1
	                    $letter = $normalizedName[0]
	                    return $letter";

            ps.AddScript(cmd);

            var results = await Task.Factory.FromAsync(ps.BeginInvoke(), x => ps.EndInvoke(x));
            
            return (char)results.First().ImmediateBaseObject;
        }
    }
}