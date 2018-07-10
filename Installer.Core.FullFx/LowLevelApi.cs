using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Installer.Core.FileSystem;
using Registry;
using Serilog;

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

            return ToDisk(this, disk);
        }

        public async Task<IList<Volume>> GetVolumes(Disk disk)
        {
            var partitions = await GetPartitions(disk);
            var partitionsObs = partitions.ToObservable();

            var volumes = partitionsObs
                .Select(x => Observable.FromAsync(async () =>
                {
                    try
                    {
                        return await GetVolume(x);
                    }
                    catch (Exception)
                    {
                        Log.Warning($"Cannot get volume for partition {x}");
                        return null;
                    }
                }))
                .Merge(1)
                .Where(v => v != null)
                .ToList();

            return await volumes;
        }

        public Task RemovePartition(Partition partition)
        {
            ps.Commands.Clear();
            var cmd = $@"Remove-Partition -DiskNumber {partition.Disk.Number} -PartitionNumber {partition.Number} -Confirm:$false";
            ps.AddScript(cmd);

            return Task.Factory.FromAsync(ps.BeginInvoke(), x => ps.EndInvoke(x));
        }

        private static Disk ToDisk(ILowLevelApi lowLevelApi, object disk)
        {
            var number = (uint)disk.GetPropertyValue("Number");
            var size = (ulong)disk.GetPropertyValue("Size");
            var allocatedSize = (ulong)disk.GetPropertyValue("AllocatedSize");

            return new Disk(lowLevelApi, number, size, allocatedSize);
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
                    var hasType = Guid.TryParse((string)x.GetPropertyValue("GptType"), out var guid);

                    return new Partition(disk)
                    {
                        Number = (uint)x.GetPropertyValue("PartitionNumber"),
                        Id = (string)x.GetPropertyValue("UniqueId"),
                        Letter = (char)x.GetPropertyValue("DriveLetter"),
                        PartitionType = hasType ? PartitionType.FromGuid(guid) : null,
                    };
                });

            return volumes.ToList();
        }

        public async Task<Volume> GetVolume(Partition partition)
        {
            ps.Commands.Clear();
            ps.AddScript($@"Get-Partition -UniqueId ""{partition.Id}"" | Get-Volume", true);

            var results = await Task.Factory.FromAsync(ps.BeginInvoke(), x => ps.EndInvoke(x));
            var volume = results.FirstOrDefault()?.ImmediateBaseObject;

            if (volume == null)
            {
                return null;
            }

            return new Volume(partition)
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

        private Partition ToPartition(Disk disk, object partition)
        {
            return new Partition(disk)
            {
                Number = (uint)partition.GetPropertyValue("PartitionNumber"),
                Id = (string)partition.GetPropertyValue("UniqueId"),
                Letter = (char)partition.GetPropertyValue("DriveLetter")
            };
        }

        public Task SetPartitionType(Partition partition, PartitionType partitionType)
        {
            ps.Commands.Clear();
            var cmd = $@"Set-Partition -PartitionNumber {partition.Number} -DiskNumber {partition.Disk.Number} -GptType ""{{{partitionType.Guid}}}""";
            ps.AddScript(cmd);

            return Task.Factory.FromAsync(ps.BeginInvoke(), x => ps.EndInvoke(x));
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
            var drives = Enumerable.Range((int) 'C', (int) 'Z').Select(i => (char)i);
            var usedDrives = DriveInfo.GetDrives().Select(x => char.ToUpper(x.Name[0]));

            var available = drives.Except(usedDrives);

            return available.First();
        }

        public bool GetIsOobeCompleted(Volume windowsVolume)
        {
            var path = Path.Combine(windowsVolume.RootDir.Name, "Windows", "System32", "Config", "System");
            var hive = new RegistryHive(path) { RecoverDeleted = true };
            hive.ParseHive();

            var key = hive.GetKey("Setup");
            var val = key.Values.Single(x => x.ValueName == "OOBEInProgress");

            return int.Parse(val.ValueData) == 0;
        }

        public async Task<Volume> GetWindowsVolume()
        {
            var disk = await GetPhoneDisk();
            var volumes = await GetVolumes(disk);
            return volumes.Single(x => x.Label == "WindowsARM");
        }
    }
}