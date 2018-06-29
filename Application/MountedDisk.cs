namespace Install
{
    public class MountedDisk
    {
        public uint DiskNumber { get; }
        public char Letter { get; }

        public MountedDisk(uint diskNumber, char letter)
        {
            DiskNumber = diskNumber;
            Letter = letter;
        }
    }
}