namespace Installer.Core
{
    public class Volume
    {
        public string Label { get; set; }
        public uint Size { get; set; }
        public Partition Partition { get; set; }
    }
}