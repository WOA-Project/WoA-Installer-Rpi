using System.Reactive;

namespace Installer.Core
{
    public class Disk
    {
        public uint Number { get; }
        public ulong Size { get; }
        public ulong AllocatedSize { get; }

        public Disk(uint number, ulong size, ulong allocatedSize)
        {
            Number = number;
            Size = size;
            AllocatedSize = allocatedSize;
        }
    }
}